using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Sharp7;
using System.Text.Json;

namespace MultiMachinePlcToSheets
{
    internal class Program
    {
        // ==============================
        //       COSTANTI PLC COMUNI
        // ==============================
        private const int PlcRack = 0;
        private const int PlcSlot = 1;
        private const int DbNumber = 55;
        private const int DbStart = 0;
        private const int DbLength = 200;

        // ----- Offsets DB55 -----
        private const int OffsetInizioSetup = 8;
        private const int OffsetFineSetup = 10;
        private const int OffsetNuovaProduzione = 12;
        private const int OffsetFineProduzione = 14;
        private const int OffsetQuantitaRaggiunta = 16;

        private const int OffsetCicliFatti = 18;
        private const int OffsetCicliScarti = 20;
        private const int OffsetNumeroOperatore = 22;
        private const int OffsetTempoMedioRil = 24;

        private const int OffsetStatoEmergenza = 34;
        private const int OffsetStatoManuale = 36;
        private const int OffsetStatoAutomatico = 38;
        private const int OffsetStatoCiclo = 40;
        private const int OffsetStatoPezziRagg = 42;
        private const int OffsetStatoAllarme = 44;

        private const int OffsetBarcodeLavorazione = 46;
        private const int OffsetQuantitaDaProd = 162;
        private const int OffsetTempoMedio = 164;
        private const int OffsetFigure = 170;

        // ==============================
        //        GOOGLE SHEETS
        // ==============================
        private const string SpreadsheetId =
            "1-SQoMJt_5tAZFlSEuSNMQLoYBsvoWXFnrCayehhx1Qg";

        private const string SheetRealtime = "PLC_REALTIME";
        private const string SheetStorico = "PLC_STORICO";

        private const string ServiceAccountJsonPath =
            @"C:\Progetti\PlcRealtimeWriter\service-account.json";

        private const int PollingIntervalMs = 4000;

        private static SheetsService _sheetsService = null!;

        // ==============================
        //        CONFIGURAZIONE
        // ==============================

        // Operatori: numero -> nome (da operatori.json)
        private static Dictionary<int, string> _operatori = new();

        // Nomi colonne, letti dai JSON (ordine = ordine di scrittura)
        private static List<string> _realtimeColumns = new();
        private static List<string> _storicoColumns = new();

        // ==============================
        //              MAIN
        // ==============================
        private static void Main(string[] args)
        {
            Console.WriteLine("Avvio MultiMachine PLC → Google Sheets...");

            try
            {
                InitGoogleSheets();

                // Caricamento JSON dinamici
                _operatori       = LoadJson<Dictionary<int, string>>("config/operatori.json");
                _realtimeColumns = LoadJson<List<string>>("config/realtime_columns.json");
                _storicoColumns  = LoadJson<List<string>>("config/storico_columns.json");

                EnsureSheetHeader(SheetRealtime, _realtimeColumns);
                EnsureSheetHeader(SheetStorico,  _storicoColumns);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERRORE IN AVVIO] " + ex.Message);
                Console.ReadKey();
                return;
            }

            // Carica configurazioni macchine dai file macchina_X.json
            var machineStates = LoadMachineStates();
            var buffer = new byte[DbLength];

            // ----------------------------
            //     LOOP INFINITO
            // ----------------------------
            while (true)
            {
                DateTime now = DateTime.Now;

                foreach (var m in machineStates)
                {
                    try
                    {
                        int result = m.Client.DBRead(DbNumber, DbStart, DbLength, buffer);
                        if (result != 0)
                        {
                            Console.WriteLine($"[PLC {m.Config.Numero}] Errore lettura DB: {result}");
                            ReconnectPlc(m);
                            continue;
                        }

                        // ====== LETTURA DATI DAL PLC ======
                        int cicliFatti      = ReadInt(buffer, OffsetCicliFatti);
                        int qtaProd         = ReadInt(buffer, OffsetQuantitaDaProd);
                        int cicliScarti     = ReadInt(buffer, OffsetCicliScarti);
                        int numeroOperatore = ReadInt(buffer, OffsetNumeroOperatore);
                        int tempoMedioRil   = ReadInt(buffer, OffsetTempoMedioRil);
                        int tempoMedio      = ReadInt(buffer, OffsetTempoMedio);
                        int figure          = ReadInt(buffer, OffsetFigure);
                        int barcodeLav      = ReadInt(buffer, OffsetBarcodeLavorazione);

                        bool nuovaProd     = ReadInt(buffer, OffsetNuovaProduzione)   != 0;
                        bool inizioSetup   = ReadInt(buffer, OffsetInizioSetup)       != 0;
                        bool fineSetup     = ReadInt(buffer, OffsetFineSetup)         != 0;
                        bool inProduzione  = ReadInt(buffer, OffsetFineProduzione)    != 0;
                        bool qtaRagg       = ReadInt(buffer, OffsetQuantitaRaggiunta) != 0;

                        // Gestione timestamp eventi 0→1
                        UpdateEvent(m.NuovaProduzione, nuovaProd,    now);
                        UpdateEvent(m.InizioSetup,     inizioSetup,  now);
                        UpdateEvent(m.FineSetup,       fineSetup,    now);
                        UpdateEvent(m.InProduzione,    inProduzione, now);

                        string statoMacchina = CalcolaStato(buffer);

                        // Nome operatore con fallback (se non trovato → numero)
                        string nomeOperatore =
                            _operatori.TryGetValue(numeroOperatore, out var nome)
                                ? nome
                                : numeroOperatore.ToString();

                        string ts = now.ToString("dd.MM.yy HH:mm:ss");

                        // ========= RIGA REALTIME =========
                        // L'ordine qui deve corrispondere a realtime_columns.json
                        var realtimeRow = new List<object>
                        {
                            ts,                         // Timestamp
                            m.Config.Numero,            // NumeroMacchina
                            barcodeLav,                 // Barcode Lavorazione
                            cicliFatti,                 // Cicli Fatti
                            qtaProd,                    // Qta da produrre
                            cicliScarti,                // Cicli Scarti
                            nomeOperatore,              // Nome Operatore
                            tempoMedioRil,              // Tempo Medio Rilevato
                            tempoMedio,                 // Tempo medio
                            statoMacchina,              // Stati macchina
                            figure,                     // Figure
                            m.NuovaProduzione.Timestamp,// Nuova Produzione
                            m.InizioSetup.Timestamp,    // Inizio Set up macchina
                            m.FineSetup.Timestamp,      // Fine Set up Macchina
                            m.InProduzione.Timestamp,   // In produzione
                            qtaRagg ? "Sì" : "No"       // Quantità raggiunta
                        };

                        WriteRealtime(m.Config.RealtimeRow, realtimeRow);

                        // ========= RIGA STORICO (solo su cambio stato/op.) =========
                        if (m.LastStato != statoMacchina || m.LastOperatore != numeroOperatore)
                        {
                            // Ordine come storico_columns.json
                            var storicoRow = new List<object>
                            {
                                ts,                         // Timestamp
                                m.Config.Numero,            // NumeroMacchina
                                barcodeLav,                 // Barcode Lavorazione
                                cicliFatti,                 // Cicli Fatti
                                qtaProd,                    // Qta da produrre
                                cicliScarti,                // Cicli Scarti
                                numeroOperatore,            // Numero Operatore
                                nomeOperatore,              // Nome Operatore
                                tempoMedioRil,              // Tempo Medio Rilevato
                                tempoMedio,                 // Tempo medio
                                statoMacchina,              // Stati macchina
                                figure,                     // Figure
                                m.NuovaProduzione.Timestamp,// Nuova Produzione
                                m.InizioSetup.Timestamp,    // Inizio Set up macchina
                                m.FineSetup.Timestamp,      // Fine Set up Macchina
                                m.InProduzione.Timestamp,   // In produzione
                                qtaRagg ? "Sì" : "No"       // Quantità raggiunta
                            };

                            AppendStorico(storicoRow);

                            m.LastStato     = statoMacchina;
                            m.LastOperatore = numeroOperatore;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERRORE Macchina {m.Config.Numero}] {ex.Message}");
                    }
                }

                Thread.Sleep(PollingIntervalMs);
            }
        }

        // ================================================================
        //                     FUNZIONI GOOGLE SHEETS
        // ================================================================
        private static void InitGoogleSheets()
        {
            GoogleCredential credential =
                GoogleCredential.FromFile(ServiceAccountJsonPath)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);

            _sheetsService = new SheetsService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "MultiMachinePlcToSheets",
                });

            Console.WriteLine("Google Sheets inizializzato.");
        }

        private static void EnsureSheetHeader(string sheet, List<string> columns)
        {
            string endColumn = ColToExcel(columns.Count);
            string range = $"{sheet}!A1:{endColumn}1";

            var resp = _sheetsService.Spreadsheets.Values.Get(SpreadsheetId, range).Execute();
            if (resp.Values != null && resp.Values.Count > 0)
                return;

            var vr = new ValueRange
            {
                Values = new List<IList<object>> { columns.Cast<object>().ToList() }
            };

            var req = _sheetsService.Spreadsheets.Values.Update(vr, SpreadsheetId, range);
            req.ValueInputOption =
                SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            req.Execute();

            Console.WriteLine($"Intestazione creata per {sheet}.");
        }

        private static void WriteRealtime(int row, List<object> data)
        {
            string endCol = ColToExcel(data.Count);
            string range = $"{SheetRealtime}!A{row}:{endCol}{row}";

            var vr = new ValueRange { Values = new List<IList<object>> { data } };

            var req = _sheetsService.Spreadsheets.Values.Update(vr, SpreadsheetId, range);
            req.ValueInputOption =
                SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            req.Execute();
        }

        private static void AppendStorico(List<object> data)
        {
            string endCol = ColToExcel(data.Count);

            var vr = new ValueRange { Values = new List<IList<object>> { data } };

            var req = _sheetsService.Spreadsheets.Values.Append(
                vr,
                SpreadsheetId,
                $"{SheetStorico}!A:{endCol}");

            req.ValueInputOption =
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            req.InsertDataOption =
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

            req.Execute();
        }

        private static string ColToExcel(int col)
        {
            string s = "";
            while (col > 0)
            {
                int mod = (col - 1) % 26;
                s = (char)(65 + mod) + s;
                col = (col - mod) / 26;
            }
            return s;
        }

        // ================================================================
        //                          PLC
        // ================================================================
        private static List<MachineState> LoadMachineStates()
        {
            var list = new List<MachineState>();

            if (!Directory.Exists("config"))
            {
                Console.WriteLine("Cartella 'config' non trovata.");
                return list;
            }

            foreach (string file in Directory.GetFiles("config", "macchina_*.json"))
            {
                try
                {
                    var cfg = LoadJson<MachineConfig>(file);
                    var ms  = new MachineState(cfg);

                    ConnectPlc(ms);
                    list.Add(ms);

                    Console.WriteLine($"Macchina {cfg.Numero} caricata ({cfg.PlcIp}) → riga realtime {cfg.RealtimeRow}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRORE CONFIG/MACCHINA] {file}: {ex.Message}");
                }
            }

            return list;
        }

        private static void ConnectPlc(MachineState m)
        {
            int r = m.Client.ConnectTo(m.Config.PlcIp, PlcRack, PlcSlot);
            if (r != 0)
                throw new Exception($"Errore connessione PLC {m.Config.Numero} ({m.Config.PlcIp}): {r}");

            Console.WriteLine($"Connesso al PLC {m.Config.Numero} ({m.Config.PlcIp})");
        }

        private static void ReconnectPlc(MachineState m)
        {
            try { m.Client.Disconnect(); } catch { /* ignore */ }
            Thread.Sleep(1000);
            try { ConnectPlc(m); } catch { /* ignore */ }
        }

        private static int ReadInt(byte[] buf, int offset)
        {
            return S7.GetIntAt(buf, offset);
        }

        private static string CalcolaStato(byte[] b)
        {
            bool emergenza = ReadInt(b, OffsetStatoEmergenza) != 0;
            bool allarme   = ReadInt(b, OffsetStatoAllarme)   != 0;
            bool manuale   = ReadInt(b, OffsetStatoManuale)   != 0;
            bool automatico= ReadInt(b, OffsetStatoAutomatico)!= 0;
            bool ciclo     = ReadInt(b, OffsetStatoCiclo)     != 0;
            bool pezzi     = ReadInt(b, OffsetStatoPezziRagg) != 0;

            if (emergenza)            return "EMERGENZA";
            if (allarme)              return "ALLARME";
            if (manuale)              return "MANUALE";
            if (automatico && ciclo)  return "AUTOMATICO - CICLO";
            if (automatico)           return "AUTOMATICO";
            if (ciclo)                return "CICLO IN CORSO";
            if (pezzi)                return "NUMERO PEZZI RAGGIUNTI";

            return "Sconosciuto";
        }

        // ================================================================
        //                STRUMENTI DI SUPPORTO
        // ================================================================
        private static T LoadJson<T>(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json)
                   ?? throw new Exception("Errore lettura JSON: " + path);
        }

        private static void UpdateEvent(EventState e, bool flag, DateTime now)
        {
            if (!e.Value && flag)
                e.Timestamp = now.ToString("dd.MM.yy HH:mm:ss");

            e.Value = flag;
        }
    }
}

using System.Net.Http.Json;

namespace PlcDashboard;

public partial class Form1 : Form
{
    private readonly HttpClient _httpClient;
    private string ApiUrl => txtApiUrl.Text.TrimEnd('/');

    public Form1()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
    }

    private async void Form1_Load(object sender, EventArgs e)
    {
        timer1.Start();
        await LoadDataAsync();
    }

    private async void btnRefresh_Click(object sender, EventArgs e)
    {
        await LoadDataAsync();
    }

    private void chkAutoRefresh_CheckedChanged(object sender, EventArgs e)
    {
        timer1.Enabled = chkAutoRefresh.Checked;
    }

    private void numRefreshInterval_ValueChanged(object sender, EventArgs e)
    {
        timer1.Interval = (int)numRefreshInterval.Value * 1000;
    }

    private async void timer1_Tick(object sender, EventArgs e)
    {
        if (chkAutoRefresh.Checked)
        {
            await LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            toolStripStatusLabel1.Text = "Caricamento...";
            toolStripStatusLabel2.Text = DateTime.Now.ToString("HH:mm:ss");
            
            var url = $"{ApiUrl}/api/Plc/realtime";
            var data = await _httpClient.GetFromJsonAsync<List<PlcRealtimeDto>>(url);
            
            if (data != null)
            {
                dataGridView1.DataSource = data;
                
                // Formattazione colonne
                if (dataGridView1.Columns.Count > 0)
                {
                    // Nascondi colonne ID
                    if (dataGridView1.Columns["MacchinaId"] != null)
                        dataGridView1.Columns["MacchinaId"]!.Visible = false;
                    
                    // Rinomina headers
                    if (dataGridView1.Columns["MacchinaNumero"] != null)
                        dataGridView1.Columns["MacchinaNumero"]!.HeaderText = "Macchina";
                    if (dataGridView1.Columns["MacchianNome"] != null)
                        dataGridView1.Columns["MacchianNome"]!.HeaderText = "Nome";
                    if (dataGridView1.Columns["StatoMacchina"] != null)
                        dataGridView1.Columns["StatoMacchina"]!.HeaderText = "Stato";
                    if (dataGridView1.Columns["CicliFatti"] != null)
                        dataGridView1.Columns["CicliFatti"]!.HeaderText = "Cicli Fatti";
                    if (dataGridView1.Columns["QuantitaDaProdurre"] != null)
                        dataGridView1.Columns["QuantitaDaProdurre"]!.HeaderText = "Da Produrre";
                    if (dataGridView1.Columns["CicliScarti"] != null)
                        dataGridView1.Columns["CicliScarti"]!.HeaderText = "Scarti";
                    if (dataGridView1.Columns["PercentualeCompletamento"] != null)
                    {
                        dataGridView1.Columns["PercentualeCompletamento"]!.HeaderText = "% Completo";
                        dataGridView1.Columns["PercentualeCompletamento"]!.DefaultCellStyle.Format = "0.0";
                    }
                    if (dataGridView1.Columns["NomeOperatore"] != null)
                        dataGridView1.Columns["NomeOperatore"]!.HeaderText = "Operatore";
                    if (dataGridView1.Columns["UltimoAggiornamento"] != null)
                    {
                        dataGridView1.Columns["UltimoAggiornamento"]!.HeaderText = "Ultimo Agg.";
                        dataGridView1.Columns["UltimoAggiornamento"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
                    }
                    
                    // Color coding per stato
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        var stato = row.Cells["StatoMacchina"]?.Value?.ToString();
                        if (stato != null)
                        {
                            if (stato.Contains("EMERGENZA"))
                                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
                            else if (stato.Contains("ALLARME"))
                                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 180);
                            else if (stato.Contains("AUTOMATICO"))
                                row.DefaultCellStyle.BackColor = Color.FromArgb(200, 255, 200);
                        }
                    }
                }
                
                toolStripStatusLabel1.Text = $"Dati caricati: {data.Count} macchine";
            }
        }
        catch (HttpRequestException ex)
        {
            toolStripStatusLabel1.Text = $"Errore connessione: {ex.Message}";
            MessageBox.Show($"Impossibile connettersi all'API.\n\nURL: {ApiUrl}/api/Plc/realtime\n\nErrore: {ex.Message}\n\nAssicurati che MESManager.Web sia in esecuzione.", 
                "Errore Connessione", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            toolStripStatusLabel1.Text = $"Errore: {ex.Message}";
            MessageBox.Show($"Errore durante il caricamento dei dati:\n\n{ex.Message}", 
                "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

public class PlcRealtimeDto
{
    public Guid MacchinaId { get; set; }
    public string MacchinaNumero { get; set; } = string.Empty;
    public string MacchianNome { get; set; } = string.Empty;
    public int CicliFatti { get; set; }
    public int QuantitaDaProdurre { get; set; }
    public int CicliScarti { get; set; }
    public int BarcodeLavorazione { get; set; }
    public int? NumeroOperatore { get; set; }
    public string? NomeOperatore { get; set; }
    public int TempoMedioRilevato { get; set; }
    public int TempoMedio { get; set; }
    public int Figure { get; set; }
    public string StatoMacchina { get; set; } = string.Empty;
    public bool QuantitaRaggiunta { get; set; }
    public DateTime UltimoAggiornamento { get; set; }
    public decimal PercentualeCompletamento { get; set; }
}

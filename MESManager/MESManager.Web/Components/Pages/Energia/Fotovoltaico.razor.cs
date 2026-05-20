using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace MESManager.Web.Components.Pages.Energia;

[Authorize]
public partial class Fotovoltaico : ComponentBase, IDisposable
{
    [Inject] private MesManagerDbContext Db { get; set; } = null!;

    private FotovoltaicoRealtime? _realtime;
    private List<ChartSeries> _chartSeries = new();
    private string[] _storicoLabels = Array.Empty<string>();
    private readonly ChartOptions _chartOptions = new() { YAxisTicks = 5 };

    private Timer? _autoRefreshTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
        // Auto-refresh ogni 30 secondi
        _autoRefreshTimer = new Timer(_ => InvokeAsync(async () =>
        {
            await LoadDataAsync();
            StateHasChanged();
        }), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private async Task LoadDataAsync()
    {
        _realtime = await Db.FotovoltaicoRealtime.FirstOrDefaultAsync(r => r.Id == 1);

        var ultimi = await Db.FotovoltaicoStorico
            .OrderByDescending(s => s.Timestamp)
            .Take(24)
            .OrderBy(s => s.Timestamp)
            .ToListAsync();

        if (ultimi.Count > 0)
        {
            _storicoLabels = ultimi.Select(s => s.Timestamp.ToString("HH:mm")).ToArray();
            _chartSeries = new List<ChartSeries>
            {
                new()
                {
                    Name = "kWh/ora",
                    Data = ultimi.Select(s => s.EnergiaOra_kWh).ToArray()
                }
            };
        }
    }

    public void Dispose()
    {
        _autoRefreshTimer?.Dispose();
    }
}

using Microsoft.AspNetCore.Components;
using Square;
using Square.Exceptions;
using Square.Models;
using SquarePayments.Data;

namespace SquarePayments.Pages;

public partial class Plans {
  [Inject]
  public SquareHelper _squareHelper { get; set; }

  private SquareClient _squareClient = null!;
  public bool Loading { get; set; } = true;
  public List<(string Id, string Description)> Cadences { get; set; } = new();
  public string NewPlanName { get; set; } = "";
  public List<CatalogObject> CatalogObjects { get; set; } = new();

  public PlanModel PlanModel { get; set; } = new() {
    Cadence = "MONTHLY"
  };

  public string Msg { get; set; } = "";

  protected override async Task OnInitializedAsync() {
    Cadences = new() {
      ("DAILY", "Once per day"),
      ("WEEKLY", "Once per week"),
      ("EVERY_TWO_WEEKS", "Every two weeks"),
      ("THIRTY_DAYS", "Once every 30 days"),
      ("SIXTY_DAYS", "Once every 60 days"),
      ("NINETY_DAYS", "Once every 90 days"),
      ("MONTHLY", "Once per month"),
      ("EVERY_TWO_MONTHS", "Once every two months"),
      ("QUARTERLY", "Once every three months"),
      ("EVERY_FOUR_MONTHS", "Once every four months"),
      ("EVERY_SIX_MONTHS", "Once every six months"),
      ("ANNUAL", "Once per year"),
      ("EVERY_TWO_YEARS", "Once every two years")
    };
    _squareClient = _squareHelper.GetSquareClient();
    await GetPlans();
    Loading = false;
  }

  private async Task GetPlans() {
    ListCatalogResponse response = await _squareClient.CatalogApi.ListCatalogAsync();
    CatalogObjects = (response.Objects ?? new List<CatalogObject>()).ToList();
  }

  private async Task CreatePlan() {
    // Yes, we should use proper validation, but for simplicity, we'll do it manually
    if (string.IsNullOrWhiteSpace(PlanModel.PlanName)) {
      Msg = "You must enter a name for this subscription plan";
      return;
    }
    if (PlanModel.Cost <= 0) {
      Msg = "You must enter a cost for this subscription plan";
      return;
    }
    List<SubscriptionPhase> phases = new() {
      new SubscriptionPhase.Builder(PlanModel.Cadence, new Money.Builder()
          .Amount((long)(PlanModel.Cost * 100))
          .Currency("GBP")
          .Build())
        .Build()
    };
    UpsertCatalogObjectRequest body = new UpsertCatalogObjectRequest.Builder(Guid.NewGuid().ToString("N"), new CatalogObject.Builder("SUBSCRIPTION_PLAN", "#new")
        .SubscriptionPlanData(new CatalogSubscriptionPlan.Builder(PlanModel.PlanName, phases)
          .Build())
        .Build())
      .Build();
    try {
      UpsertCatalogObjectResponse result = await _squareClient.CatalogApi.UpsertCatalogObjectAsync(body);
      Msg = "Success, reload the page to see it";
    }
    catch (ApiException ex) {
      Msg = $"Error: {string.Join("", ex.Errors.Select(e => $"({e.Field}) {e.Detail}"))}";
    }
    catch (Exception ex) {
      Msg = $"Error: {ex.Message}";
    }
  }

  private async Task DeletePlan(string id) {
    try {
      DeleteCatalogObjectResponse result = await _squareClient.CatalogApi.DeleteCatalogObjectAsync(id);
      if (result.Errors.Any()) {
        Msg = $"Error: {string.Join("", result.Errors.Select(e => $"({e.Field}) {e.Detail}"))}";

      } else {
        CatalogObjects.Remove(CatalogObjects.Single(co => co.Id == id));
        Msg = "Plan removed";
      }
    } catch (ApiException ex) {
      Msg = $"ApiException: {string.Join("", ex.Errors.Select(e => $"({e.Field}) {e.Detail}"))}";
    } catch (Exception ex) {
      Msg = $"Exception: {ex.Message}";
    }
    StateHasChanged();
  }
}

public class PlanModel {
  public string PlanName { get; set; } = "";
  public string Cadence { get; set; } = "";
  public decimal Cost { get; set; }
}
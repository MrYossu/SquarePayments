using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Pixata.Extensions;
using Square;
using Square.Exceptions;
using Square.Models;
using SquarePayments.Data;

namespace SquarePayments.Pages;

public partial class Customers : IAsyncDisposable {
  [Inject]
  public IJSRuntime _js { get; set; }

  [Inject]
  public SquareHelper _squareHelper { get; set; }

  private SquareClient _client = null!;
  private IJSObjectReference _squareJs = null!;
  private IJSObjectReference _squareCard = null!;
  private readonly Square.Environment _environment = Square.Environment.Sandbox;
  private readonly string _elementId = "SquareCardPayment" + Guid.NewGuid().ToString("N");
  public CustomerDto Customer { get; set; } = new();
  public List<Customer> CustomerList { get; set; } = new();
  public string CustomerListMsg { get; set; } = "";
  public List<CatalogObject> CatalogObjects { get; set; } = new();

  protected override async Task OnInitializedAsync() {
    _client = _squareHelper.GetSquareClient();
    await GetPlans();
    Customer.PlanId = CatalogObjects.First().Id;
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      try {
        await _js.InvokeAsync<IJSObjectReference>("import", _environment == Square.Environment.Sandbox ? "https://sandbox.web.squarecdn.com/v1/square.js" : "https://web.squarecdn.com/v1/square.js");
        _squareJs = await _js.InvokeAsync<IJSObjectReference>("import", "/Square.js");
        _squareCard = await _squareJs.InvokeAsync<IJSObjectReference>("addSquareCardPayment", _elementId, _squareHelper.AppId, _squareHelper.LocationId);
        StateHasChanged();
      }
      catch (Exception ex) {
        NewCustomerMsg = $"Exception: {ex.Message}";
        StateHasChanged();
      }
    }
  }

  private async Task GetPlans() {
    ListCatalogResponse response = await _client.CatalogApi.ListCatalogAsync();
    CatalogObjects = (response.Objects ?? new List<CatalogObject>()).ToList();
  }

  private async Task ListCustomers() {
    try {
      CustomerList = (await _client.CustomersApi.ListCustomersAsync()).Customers.ToList();
    }
    catch (ApiException ex) {
      CustomerListMsg = $"{ex.GetType().Name}: {ex.Errors.Select(e => $"({e.Category}) {e.Detail}").JoinStr()}";
    }
    catch (Exception ex) {
      CustomerListMsg = $"{ex.GetType().Name}: {ex.Message}";
    }
  }

  public string NewCustomerMsg { get; set; } = "";

  private async Task CreateCustomer() {
    NewCustomerMsg = "Please wait...";
    try {
      Address address = SquareHelper.BuildAddress(Customer.Address1, Customer.Address2, Customer.Postcode, Customer.Country);
      string customerId = await _squareHelper.CreateSquareCustomer(Customer.FirstName, Customer.Surname, Customer.Email, Customer.Phone, address);
      string cardId = await CreateSquareCard(Customer.FirstName, Customer.Surname, address, customerId);
      string subscriptionId = await CreateSquareSubscription(customerId, cardId);
      NewCustomerMsg = $"Success: Id {subscriptionId}";
    }
    catch (ApiException ex) {
      NewCustomerMsg = $"Error: {string.Join("", ex.Errors.Select(e => $"({e.Field}) {e.Detail}"))}";
    }
    catch (Exception ex) {
      NewCustomerMsg = $"Error: {ex.Message}";
    }
  }

  public async Task<string> CreateSquareCard(string firstName, string surname, Address address, string customerId) {
    string sourceId = await _squareJs.InvokeAsync<string>("getSquareCardToken", _squareCard);
    // NOTE - The following will throw an error if the postcode/zip used in the card element does not match the one you use in the address
    CreateCardRequest cardRequest = new CreateCardRequest.Builder(Guid.NewGuid().ToString(), sourceId,
        new Card.Builder()
          .CardholderName($"{firstName} {surname}")
          .BillingAddress(address)
          .CustomerId(customerId)
          .ReferenceId($"{firstName}{surname}")
          .Build())
      .Build();
    CreateCardResponse cardResponse = await _client.CardsApi.CreateCardAsync(cardRequest);
    return cardResponse.Card.Id;
  }

  private async Task<string> CreateSquareSubscription(string customerId, string cardId) {
    CreateSubscriptionRequest subscriptionRequest = new CreateSubscriptionRequest.Builder(_squareHelper.LocationId, Customer.PlanId, customerId)
      .IdempotencyKey(Guid.NewGuid().ToString("N"))
      .CardId(cardId)
      .Build();
    CreateSubscriptionResponse subscriptionResponse = await _client.SubscriptionsApi.CreateSubscriptionAsync(subscriptionRequest);
    return subscriptionResponse.Subscription.Id;
  }

  private async Task DeleteCustomer(string id) {
    try {
      await _client.CustomersApi.DeleteCustomerAsync(id);
      NewCustomerMsg = "Deleted";
    }
    catch (ApiException ex) {
      NewCustomerMsg = $"Error: {string.Join("", ex.Errors.Select(e => $"({e.Field}) {e.Detail}"))}";
    }
    catch (Exception ex) {
      NewCustomerMsg = $"Error: {ex.Message}";
    }
  }

  public async ValueTask DisposeAsync() {
    if (_squareJs is not null) {
      await _squareJs.DisposeAsync();
    }
  }
}
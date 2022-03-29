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

  private SquareClient _squareClient = null!;
  private IJSObjectReference _squareJs = null!;
  private IJSObjectReference _squareCard = null!;
  private readonly Square.Environment _environment = Square.Environment.Sandbox;
  private readonly string _elementId = "SquareCardPayment" + Guid.NewGuid().ToString("N");
  public CustomerDto Customer { get; set; } = new();
  public List<Customer> CustomerList { get; set; } = new();
  public string CustomerListMsg { get; set; } = "";
  public List<CatalogObject> CatalogObjects { get; set; } = new();

  protected override async Task OnInitializedAsync() {
    _squareClient = _squareHelper.GetSquareClient();
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
    ListCatalogResponse response = await _squareClient.CatalogApi.ListCatalogAsync();
    CatalogObjects = (response.Objects ?? new List<CatalogObject>()).ToList();
  }

  private async Task ListCustomers() {
    try {
      CustomerList = (await _squareClient.CustomersApi.ListCustomersAsync()).Customers.ToList();
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
      Address address = new Address.Builder()
        .AddressLine1(Customer.Address1)
        .AddressLine2(Customer.Address2)
        .PostalCode(Customer.Postcode)
        .Country(Customer.Country)
        .Build();
      CreateCustomerRequest customerRequest = new CreateCustomerRequest.Builder()
        .IdempotencyKey(Guid.NewGuid().ToString())
        .GivenName(Customer.FirstName)
        .FamilyName(Customer.Surname)
        .EmailAddress(Customer.Email)
        .Address(address)
        .PhoneNumber(Customer.Phone)
        .Build();
      CreateCustomerResponse customerResponse = await _squareClient.CustomersApi.CreateCustomerAsync(customerRequest);
      string customerId = customerResponse.Customer.Id;
      string sourceId = await _squareJs.InvokeAsync<string>("getSquareCardToken", _squareCard);
      // NOTE - The following will throw an error if the postcode/zip used in the card element does not match the one you use in the address
      CreateCardRequest cardRequest = new CreateCardRequest.Builder(Guid.NewGuid().ToString(), sourceId,
          new Card.Builder()
            .CardholderName($"{Customer.FirstName} {Customer.Surname}")
            .BillingAddress(address)
            .CustomerId(customerId)
            .ReferenceId($"{Customer.FirstName}{Customer.Surname}")
            .Build())
        .Build();
      CreateCardResponse cardResponse = await _squareClient.CardsApi.CreateCardAsync(cardRequest);
      string cardId = cardResponse.Card.Id;
      CreateSubscriptionRequest subscriptionRequest = new CreateSubscriptionRequest.Builder(_squareHelper.LocationId, Customer.PlanId, customerId)
        .IdempotencyKey(Guid.NewGuid().ToString("N"))
        .CardId(cardId)
        .Build();
      CreateSubscriptionResponse subscriptionResponse = await _squareClient.SubscriptionsApi.CreateSubscriptionAsync(subscriptionRequest);
      string subscriptionId = subscriptionResponse.Subscription.Id;
      NewCustomerMsg = $"Success: Id {subscriptionId}";
    }
    catch (ApiException ex) {
      NewCustomerMsg = $"Error: {string.Join("", ex.Errors.Select(e => $"({e.Field}) {e.Detail}"))}";
    }
    catch (Exception ex) {
      NewCustomerMsg = $"Error: {ex.Message}";
    }
  }

  private async Task DeleteCustomer(string id) {
    try {
      await _squareClient.CustomersApi.DeleteCustomerAsync(id);
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
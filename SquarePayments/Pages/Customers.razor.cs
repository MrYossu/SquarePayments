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

  protected override void OnInitialized() =>
    _squareClient = _squareHelper.GetSquareClient();

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      try {
        await _js.InvokeAsync<IJSObjectReference>("import", _environment == Square.Environment.Sandbox ? "https://sandbox.web.squarecdn.com/v1/square.js" : "https://web.squarecdn.com/v1/square.js");
        _squareJs = await _js.InvokeAsync<IJSObjectReference>("import", "/Square.js");
        _squareCard = await _squareJs.InvokeAsync<IJSObjectReference>("addSquareCardPayment", _elementId, _squareHelper.AppId, _squareHelper.LocationId);
        StateHasChanged();
      }
      catch (Exception ex) {
        StateHasChanged();
      }
    }
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
      Address? address = new Address.Builder()
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
      string sourceId = await _squareJs.InvokeAsync<string>("getSquareCardToken", _squareCard);
      // We should pass sourceId as the second parameter below, but it throws an error when we do that.
      // Actually, it throws an error even with the sandbox source Id (see bullet point 2b in section 1.3
      // of the subscription doc https://developer.squareup.com/docs/subscriptions-api/walkthrough#step-13-create-customers-in-the-sellers-customer-directory)
      CreateCardRequest cardRequest = new CreateCardRequest.Builder(Guid.NewGuid().ToString(), "cnon:card-nonce-ok",
          new Card.Builder()
            .CardholderName($"{Customer.FirstName} {Customer.Surname}")
            .BillingAddress(address)
            .CustomerId(customerResponse.Customer.Id)
            .ReferenceId($"{Customer.FirstName}{Customer.Surname}")
            .Build())
        .Build();
      CreateCardResponse cardResponse = await _squareClient.CardsApi.CreateCardAsync(cardRequest);
      NewCustomerMsg = "Success";
    }
    catch (ApiException ex) {
      NewCustomerMsg = $"Error: {string.Join("", ex.Errors.Select(e => $"({e.Field}) {e.Detail}"))}";
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
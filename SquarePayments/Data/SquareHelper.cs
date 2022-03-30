using Microsoft.JSInterop;
using Pixata.Extensions;
using Square;
using Square.Models;

namespace SquarePayments.Data;

public class SquareHelper {
  private readonly IJSRuntime _js;
  private readonly SquareData _data;
  private readonly SquareClient _client;
  private IJSObjectReference? _squareJs;
  private IJSObjectReference? _squareCard;
  private string _elementId = "";

  public static string JsUri = "/Square.js";

  public SquareHelper(SquareData data, IJSRuntime js) {
    _js = js;
    _data = data;
    _client = new SquareClient.Builder()
      .Environment(_data.Environment == "production" ? Square.Environment.Production : Square.Environment.Sandbox)
      .AccessToken(_data.AccessToken)
      .Build();
  }

  #region API methods

  public static Address BuildAddress(string address1, string address2, string postcode, string country) {
    Address address = new Address.Builder()
      .AddressLine1(address1)
      .AddressLine2(address2)
      .PostalCode(postcode)
      .Country(country)
      .Build();
    return address;
  }

  public async Task<string> CreateSquareCustomer(string firstName, string surname, string email, string phone, Address address) {
    CreateCustomerRequest customerRequest = new CreateCustomerRequest.Builder()
      .IdempotencyKey(Guid.NewGuid().ToString())
      .GivenName(firstName)
      .FamilyName(surname)
      .EmailAddress(email)
      .Address(address)
      .PhoneNumber(phone)
      .Build();
    CreateCustomerResponse customerResponse = await _client.CustomersApi.CreateCustomerAsync(customerRequest);
    return customerResponse.Customer.Id;
  }

  public async Task SetUpCard(string elementId) {
    _elementId = elementId;
    await _js.InvokeAsync<IJSObjectReference>("import", _data.Environment == "production" ? "https://web.squarecdn.com/v1/square.js" : "https://sandbox.web.squarecdn.com/v1/square.js");
    _squareJs = await _js.InvokeAsync<IJSObjectReference>("import", JsUri);
    _squareCard = await _squareJs.InvokeAsync<IJSObjectReference>("addSquareCardPayment", _elementId, _data.AppId, _data.LocationId);
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

  public async Task<string> CreateSquareSubscription(string customerId, string cardId, string planId, int dayOfMonth = 0) {
    DateTime start = dayOfMonth == 0 ? DateTime.Now : DateTime.Now.EndOfMonth().AddMilliseconds(2);
    CreateSubscriptionRequest subscriptionRequest = new CreateSubscriptionRequest.Builder(_data.LocationId, planId, customerId)
      .IdempotencyKey(Guid.NewGuid().ToString("N"))
      .CardId(cardId)
      .StartDate(start.ToString("yyyy-MM-dd"))
      .Build();
    CreateSubscriptionResponse subscriptionResponse = await _client.SubscriptionsApi.CreateSubscriptionAsync(subscriptionRequest);
    return subscriptionResponse.Subscription.Id;
  }

  public async Task<List<Customer>> GetAllCustomers() {
    List<Customer> results = new();
    ListCustomersResponse customerResponse = await _client.CustomersApi.ListCustomersAsync();
    results.AddRange(customerResponse.Customers);
    string cursor = customerResponse.Cursor;
    while (!string.IsNullOrWhiteSpace(cursor)) {
      customerResponse = await _client.CustomersApi.ListCustomersAsync(cursor);
      if (customerResponse.Customers == null) {
        break;
      }
      results.AddRange(customerResponse.Customers);
      cursor = customerResponse.Cursor;
    }
    return results;
  }

  public async Task<List<Customer>> SearchCustomers(string email) {
    SearchCustomersRequest.Builder body = new SearchCustomersRequest.Builder()
      .Query(new CustomerQuery.Builder()
        .Filter(new CustomerFilter.Builder()
          .EmailAddress(new CustomerTextFilter.Builder()
            .Fuzzy(email)
            .Build())
          .Build())
        .Build());
    List<Customer> results = new();
    SearchCustomersResponse customerResponse = await _client.CustomersApi.SearchCustomersAsync(body.Build());
    if (customerResponse.Customers == null) {
      return results;
    }
    results.AddRange(customerResponse.Customers);
    string cursor = customerResponse.Cursor;
    while (!string.IsNullOrWhiteSpace(cursor)) {
      customerResponse = await _client.CustomersApi.SearchCustomersAsync(body.Cursor(cursor).Build());
      if (customerResponse.Customers == null) {
        break;
      }
      results.AddRange(customerResponse.Customers);
      cursor = customerResponse.Cursor;
    }
    return results;
  }

  public async Task<DeleteCustomerResponse> DeleteCustomer(string id) =>
    await _client.CustomersApi.DeleteCustomerAsync(id);

  public async Task<(string OrderId, string PaymentId)> OrderWithPayment(string customerId, List<OrderLineItem> lineItems, string reference) {
    CreateOrderRequest orderRequest = new CreateOrderRequest.Builder()
      .Order(new Order.Builder(_data.LocationId)
        .CustomerId(customerId)
        .LineItems(lineItems)
        .Build())
      .IdempotencyKey(Guid.NewGuid().ToString())
      .Build();
    CreateOrderResponse orderResponse = await _client.OrdersApi.CreateOrderAsync(orderRequest);
    string orderId = orderResponse.Order.Id;
    long total = lineItems.Select(li => Convert.ToInt32(li.Quantity) * (li.BasePriceMoney.Amount ?? 0)).Sum(t => t);
    CreatePaymentRequest paymentRequest = new CreatePaymentRequest.Builder(
        await _squareJs.InvokeAsync<string>("getSquareCardToken", _squareCard), Guid.NewGuid().ToString(), new Money.Builder()
          .Amount(total)
          .Currency("GBP")
          .Build())
      .Autocomplete(true)
      .OrderId(orderId)
      .ReferenceId(reference)
      .Build();
    CreatePaymentResponse result = await _client.PaymentsApi.CreatePaymentAsync(paymentRequest);
    return (orderId, result.Payment.Id);
  }

  #endregion

  #region Deprecated

  // Move all the Square API calls inside this class and remove these three...
  public SquareClient GetSquareClient() =>
    _client;

  public string AppId =>
    _data.AppId;

  public string LocationId =>
    _data.LocationId;

  #endregion
}
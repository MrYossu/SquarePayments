using Microsoft.JSInterop;
using Square;
using Square.Models;
using Environment = System.Environment;

namespace SquarePayments.Data;

public class SquareHelper {
  private readonly IJSRuntime _js;
  private readonly SquareData _data;
  private readonly SquareClient _client;
  private IJSObjectReference? _squareJs;
  private IJSObjectReference? _squareCard;
  private string _elementId = "";

  public static string JsUri = "/Square.js";

  public SquareHelper(IJSRuntime js) {
    _js = js;
    // NOTE - I am using environment variables for convenience here. In a real app you would use appSettings.json, Azure secrets or the like
    _data = new() {
      Environment = "sandbox",
      AccessToken = Environment.GetEnvironmentVariable("SquareAccessToken") ?? "",
      AppId = Environment.GetEnvironmentVariable("SquareAppId") ?? "",
      LocationId = Environment.GetEnvironmentVariable("SquareLocationId") ?? ""
    };
    _client = new SquareClient.Builder()
      .Environment(_data.Environment == "production" ? Square.Environment.Production : Square.Environment.Sandbox)
      .AccessToken(_data.AccessToken)
      .Build();
  }

  private Square.Environment GetEnvironment =>
    _data.Environment == "production"
      ? Square.Environment.Production
      : Square.Environment.Sandbox;

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
    await _js.InvokeAsync<IJSObjectReference>("import", GetEnvironment == Square.Environment.Sandbox ? "https://sandbox.web.squarecdn.com/v1/square.js" : "https://web.squarecdn.com/v1/square.js");
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

  public async Task<string> CreateSquareSubscription(string customerId, string cardId, string planId) {
    CreateSubscriptionRequest subscriptionRequest = new CreateSubscriptionRequest.Builder(_data.LocationId, planId, customerId)
      .IdempotencyKey(Guid.NewGuid().ToString("N"))
      .CardId(cardId)
      .Build();
    CreateSubscriptionResponse subscriptionResponse = await _client.SubscriptionsApi.CreateSubscriptionAsync(subscriptionRequest);
    return subscriptionResponse.Subscription.Id;
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
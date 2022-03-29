using Square;
using Square.Models;
using Environment = System.Environment;

namespace SquarePayments.Data;

public class SquareHelper {
  private readonly SquareData _data;
  private readonly SquareClient _client;

  public SquareHelper() {
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

  #endregion

  // Move all the Square API calls inside this class and remove these three...
  public SquareClient GetSquareClient() =>
    _client;

  public string AppId =>
    _data.AppId;

  public string LocationId =>
    _data.LocationId;
}
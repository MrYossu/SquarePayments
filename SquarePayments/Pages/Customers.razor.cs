using Microsoft.AspNetCore.Components;
using Pixata.Extensions;
using Square;
using Square.Exceptions;
using Square.Models;
using SquarePayments.Data;

namespace SquarePayments.Pages;

public partial class Customers {
  [Inject]
  public SquareHelper SquareHelper { get; set; } = null!;

  // TODO AYS - Move GetPlans and GetCustomers to the helper and remove this
  private SquareClient _client = null!;
  private readonly string _elementId = "SquareCardPayment" + Guid.NewGuid().ToString("N");
  public CustomerDto Customer { get; set; } = new();
  public List<Customer> CustomerList { get; set; } = new();
  public string CustomerListMsg { get; set; } = "";
  public List<CatalogObject> CatalogObjects { get; set; } = new();

  protected override async Task OnInitializedAsync() {
    _client = SquareHelper.GetSquareClient();
    await GetPlans();
    Customer.PlanId = CatalogObjects.First().Id;
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      try {
        await SquareHelper.SetUpCard(_elementId);
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

  private async Task ListCustomers() =>
    CustomerList = await SquareHelper.GetAllCustomers();

  public string NewCustomerMsg { get; set; } = "";

  private async Task CreateCustomer() {
    NewCustomerMsg = "Please wait...";
    try {
      Address address = SquareHelper.BuildAddress(Customer.Address1, Customer.Address2, Customer.Postcode, Customer.Country);
      string customerId = await SquareHelper.CreateSquareCustomer(Customer.FirstName, Customer.Surname, Customer.Email, Customer.Phone, address);
      string cardId = await SquareHelper.CreateSquareCard(Customer.FirstName, Customer.Surname, address, customerId);
      string subscriptionId = await SquareHelper.CreateSquareSubscription(customerId, cardId, Customer.PlanId, 1);
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
      DeleteCustomerResponse response = await SquareHelper.DeleteCustomer(id);
      NewCustomerMsg = "Deleted";
    }
    catch (ApiException ex) {
      NewCustomerMsg = $"Error: {string.Join("", ex.Errors.Select(e => $"({e.Field}) {e.Detail}"))}";
    }
    catch (Exception ex) {
      NewCustomerMsg = $"Error: {ex.Message}";
    }
  }
}
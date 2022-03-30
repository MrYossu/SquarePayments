using Microsoft.AspNetCore.Components;
using Square;
using Square.Exceptions;
using Square.Models;
using SquarePayments.Data;

namespace SquarePayments.Pages;

public partial class OrderAndPay {
  [Inject]
  private SquareHelper SquareHelper { get; set; } = null!;

  private SquareClient _squareClient = null!;
  public List<Customer> Customers { get; set; } = new();
  public string CustomerId { get; set; } = "";
  private int _small;
  private int _medium;
  private int _enormous;
  public double Total { get; set; }
  private readonly string _elementId = "SquareCardPayment" + Guid.NewGuid().ToString("N");
  public bool SquareLoaded { get; set; }
  public string PaymentMsg { get; set; } = "Please choose some chocolate before ordering";

  protected override async Task OnInitializedAsync() {
    _squareClient = SquareHelper.GetSquareClient();
    List<Customer> customers = (await _squareClient.CustomersApi.ListCustomersAsync()).Customers.ToList();
    customers.Insert(0, new("", givenName: "(please choose)"));
    Customers = customers;
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      try {
        await SquareHelper.SetUpCard(_elementId);
        SquareLoaded = true;
        StateHasChanged();
      }
      catch (Exception ex) {
        PaymentMsg = $"Exception ({ex.GetType().Name}): {ex.Message}";
        StateHasChanged();
      }
    }
  }

  private void UpdateTotal(char size, string qty) {
    switch (size) {
      case 's':
        _small = Convert.ToInt32(qty);
        break;
      case 'm':
        _medium = Convert.ToInt32(qty);
        break;
      case 'e':
        _enormous = Convert.ToInt32(qty);
        break;
    }
    Total = _small + 2 * _medium + 3.5 * _enormous;
    PaymentMsg = Total > 0 ? "Please click Pay to place your order" : "Please choose some chocolate before ordering";
  }

  private async Task Pay() {
    if (string.IsNullOrWhiteSpace(CustomerId)) {
      PaymentMsg = "Please choose a customer";
      return;
    }
    List<OrderLineItem> lineItems = new();
    if (_small > 0) {
      lineItems.Add(new OrderLineItem.Builder(_small.ToString())
        .Name("Bar of chocolate (small)")
        .Quantity(_small.ToString())
        .BasePriceMoney(new Money.Builder()
          .Amount(100L)
          .Currency("GBP")
          .Build())
        .Build());
    }
    if (_medium > 0) {
      lineItems.Add(new OrderLineItem.Builder(_medium.ToString())
        .Name("Bar of chocolate (medium)")
        .Quantity(_medium.ToString())
        .BasePriceMoney(new Money.Builder()
          .Amount(200L)
          .Currency("GBP")
          .Build())
        .Build());
    }
    if (_enormous > 0) {
      lineItems.Add(new OrderLineItem.Builder(_enormous.ToString())
        .Name("Bar of chocolate (enormous)")
        .Quantity(_enormous.ToString())
        .BasePriceMoney(new Money.Builder()
          .Amount(350L)
          .Currency("GBP")
          .Build())
        .Build());
    }

    try {
      (string orderId, string paymentId) = await SquareHelper.OrderWithPayment(CustomerId, lineItems, $"ChocShop-{DateTime.Now.ToString("yymmddHHmmss")}");
      PaymentMsg = $"Success, order Id: {orderId}, payment Id: {paymentId}";
    }
    catch (ApiException ex) {
      PaymentMsg = "ApiException: " + string.Join("", ex.Errors.Select(e => e.Detail));
    }
    catch (Exception ex) {
      PaymentMsg = $"HTTP error(s): {ex.Message}";
    }
  }
}
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Square;
using Square.Exceptions;
using Square.Models;
using SquarePayments.Data;

namespace SquarePayments.Pages;

public partial class OrderAndPay : IAsyncDisposable {
  [Inject]
  private SquareHelper _squareHelper { get; set; }

  [Inject]
  private IJSRuntime _js { get; set; }

  private SquareClient _squareClient = null!;
  public List<Customer> Customers { get; set; } = new();
  public string CustomerId { get; set; } = "";
  private int _small;
  private int _medium;
  private int _enormous;
  public double Total { get; set; }
  private IJSObjectReference _squareJs = null!;
  private IJSObjectReference _squareCard = null!;
  private readonly Square.Environment _environment = Square.Environment.Sandbox;
  private readonly string _elementId = "SquareCardPayment" + Guid.NewGuid().ToString("N");
  public bool SquareLoaded { get; set; }
  public string PaymentMsg { get; set; } = "Please choose some chocolate before ordering";

  protected override async Task OnInitializedAsync() {
    _squareClient = _squareHelper.GetSquareClient();
    List<Customer> customers = (await _squareClient.CustomersApi.ListCustomersAsync()).Customers.ToList();
    customers.Insert(0, new Customer(id: "", givenName: "(please choose)"));
    Customers = customers;
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      try {
        await _js.InvokeAsync<IJSObjectReference>("import", _environment == Square.Environment.Sandbox ? "https://sandbox.web.squarecdn.com/v1/square.js" : "https://web.squarecdn.com/v1/square.js");
        _squareJs = await _js.InvokeAsync<IJSObjectReference>("import", "/Square.js");
        _squareCard = await _squareJs.InvokeAsync<IJSObjectReference>("addSquareCardPayment", _elementId, _squareHelper.AppId, _squareHelper.LocationId);
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
        .BasePriceMoney(new Money.Builder()
          .Amount(100L)
          .Currency("GBP")
          .Build())
        .Build());
    }
    if (_medium > 0) {
      lineItems.Add(new OrderLineItem.Builder(_medium.ToString())
        .Name("Bar of chocolate (medium)")
        .BasePriceMoney(new Money.Builder()
          .Amount(200L)
          .Currency("GBP")
          .Build())
        .Build());
    }
    if (_enormous > 0) {
      lineItems.Add(new OrderLineItem.Builder(_enormous.ToString())
        .Name("Bar of chocolate (enormous)")
        .BasePriceMoney(new Money.Builder()
          .Amount(350L)
          .Currency("GBP")
          .Build())
        .Build());
    }
    Order order = new Order.Builder(_squareHelper.LocationId)
      .CustomerId(CustomerId)
      .LineItems(lineItems)
      .Build();

    CreateOrderRequest orderRequest = new CreateOrderRequest.Builder()
      .Order(order)
      .IdempotencyKey(Guid.NewGuid().ToString())
      .Build();

    try {
      CreateOrderResponse orderResponse = await _squareClient.OrdersApi.CreateOrderAsync(orderRequest);
      string orderId = orderResponse.Order.Id;
      CreatePaymentRequest paymentRequest = new CreatePaymentRequest.Builder(
          await _squareJs.InvokeAsync<string>("getSquareCardToken", _squareCard), Guid.NewGuid().ToString(), new Money.Builder()
            .Amount((int)(Total * 100))
            .Currency("GBP")
            .Build())
        .Autocomplete(true)
        .OrderId(orderId)
        .ReferenceId($"ChocShop-{DateTime.Now.ToString("yymmddHHmmss")}")
        .Build();
      CreatePaymentResponse result = await _squareClient.PaymentsApi.CreatePaymentAsync(paymentRequest);
      PaymentMsg = $"Success, order Id: {orderId}";
    }
    catch (ApiException ex) {
      PaymentMsg = "ApiException: " + string.Join("", ex.Errors.Select(e => e.Detail));
    }
    catch (Exception ex) {
      PaymentMsg = $"HTTP error(s): {ex.Message}";
    }
  }

  public async ValueTask DisposeAsync() {
    if (_squareJs != null) {
      await _squareJs.DisposeAsync();
    }
  }
}
using Square.Models;

namespace SquarePayments.Data; 

public class OrderDto {
  public string LocationId { get; set; } = "";
  public string CustomerId { get; set; } = "";
  public OrderLineItem[] Items { get; set; }
  public string Reference { get; set; } = "";

}
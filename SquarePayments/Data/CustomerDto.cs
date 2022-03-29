using System.ComponentModel.DataAnnotations;

namespace SquarePayments.Data; 

public class CustomerDto {
  public string FirstName { get; set; } = "";
  public string Surname { get; set; } = "";
  [EmailAddress]
  public string Email { get; set; } = "";
  public string Phone { get; set; } = "";
  public string Address1 { get; set; } = "";
  public string Address2 { get; set; } = "";
  public string Postcode { get; set; } = "";
  public string Country { get; set; } = "GB";
  public string PlanId { get; set; } = "";
}
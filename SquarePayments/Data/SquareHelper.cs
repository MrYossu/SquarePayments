using Square;
using Environment = System.Environment;

namespace SquarePayments.Data;

public class SquareHelper {
  public const string SquareAppId = "SquareAppId";
  public const string SquareAccessToken = "SquareAccessToken";
  public const string SquareLocationId = "SquareLocationId";

  public SquareClient GetSquareClient() => new SquareClient.Builder()
    .Environment(Square.Environment.Sandbox)
    .AccessToken(Environment.GetEnvironmentVariable(SquareAccessToken))
    .Build();

  public string AppId =>
    Environment.GetEnvironmentVariable(SquareAppId) ?? "";

  public string LocationId =>
    Environment.GetEnvironmentVariable(SquareLocationId) ?? "";
}
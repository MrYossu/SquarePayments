# SquarePayments
## Sample code for working with the Square API from Blazor

The pages here show how to manage customers, take payments, etc in a Blazor app, using the Square API.

## Set up
In order to use this code, you need an account with Square (duh). Then you need to create the data object and register both it and the helper class as follows...

```c#
// .NET5 - Goes in Startup.cs
SquareData squareData = Configuration.GetSection("Square").Get<SquareData>();
services.AddSingleton(squareData);
services.AddTransient<SquareHelper>();

// .NET6 - Goes in Program.cs
SquareData SquareData = builder.Configuration.GetSection("Square").Get<SquareData>();
builder.Services.AddSingleton(SquareData);
builder.Services.AddTransient<SquareHelper>();
```

Note that the sample code in the repository uses three environment variables so that I could commit the code without having to remember to blank out my credentials each time (see the home page of the project for details of how to set these up). In practice, you would use `appSettings.json`, Azure secrets or the like. If you are playing with this code, you could just hard-code the data as follows (all data created by me randomly bashing the keyboard!)...

```c#
SquareData data = new() {
  Environment = "sandbox",
  AccessToken = "abcdefgh12345",
  AppId = "93879837492378429378",
  LocationId = "2u289d9283jd92dj8923d8"
};
```

Once that is done, you can inject a `SquareHelper` into your class and you are (mostly) ready to go. If you want to take card payments on the page, you will also need to initialise the card element. You need a `string` property for the element Id...

```c#
private readonly string _elementId = "SquareCardPayment" + Guid.NewGuid().ToString("N");
```

You don't ahve to use a `Guid`, just make sure that the Id is unique. Then, you need to add a `<div>` to hold the card element...

```html
<div id="@_elementId"></div>
```

Finally, you need to initialise the card element...

```c#
  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      try {
        await SquareHelper.SetUpCard(_elementId);
      }
      catch (Exception ex) {
        Msg = $"Exception: {ex.Message}";
      }
    }
  }
```

The code above assumes you have a `string` property named `Msg` in your component, and ensures that if anything goes wrong, you see what it was (assuming you bind `Msg` to something in the view of course). In practice, I find that once you have the code set up, you can do away with this, but it doesn't harm to keep it. If you want to be professional, you would use some better way of informing the user that the card payment facility was unavailable. As with all the other code in this project, this is to demonstrate the concepts, so there is always room for improvement.

## Caveat
The code here is just to show how it can be done. In practice, you would want to make some improvements. Obvious ones are...

- There is a noticeable delay on some pages before the Square controls appear on the page. We should show a busy indicator (or the like) while this is happening.
- When you click the buttons on most pages, there is little indication that anything is happening. Again, disabling the controls and showing a busy indicator would help.

## Helper class
In order to make life easier, I moved a lot of the Square code into an imaginatively named [`SquareHelper` class](https://github.com/MrYossu/SquarePayments/blob/master/SquarePayments/Data/SquareHelper.cs). The idea is to hde as much of the actual Square code as possible, leaving the calling code as clean as it can. This is a work in progress (like everything else here), so there are still paces where the calls are made manually from a local `SquareClient` object, rather than using the helper. The [Customers page](https://github.com/MrYossu/SquarePayments/blob/master/SquarePayments/Pages/Customers.razor.cs) is probably the best example of using the helper, although that still has (at the time of writing) some calls to a local client.

## Explanation of what's in this project
The code shown here is the result of me experimenting with the Square API. I do not make any claims that this is the only way to use it, nor even that it's the best way to use it. This is what I managed to get working. If you have any (polite!) comments on my code, please feel free to open an issue on the [GitHub repository](https://github.com/MrYossu/SquarePayments).

Also, the UI design here won't win any prizes. I'm a software developer, not a graphic designer, and these pages were written to test a concept, so they don't look very pretty!

Caveat aside, here is a brief explanation of the pages in the project:

### Index
Some info about the project (mainly the same as what is here), but also shows you your environment variables, so yo can check that they are set correctly.

### Payments
This was my first attempt at taking payments, and was based on Alfred Zaki's code (see above). It does nothing more than take the payment, it does not create customers or orders, etc. This approach would be fine if you were dealing with a lot of small transactions from a lot of different people, but probably wouldn't be so good for an ecommerce site.

### Customers
List customers, and create a new one. Fairly simple and self-explanatory. The original page has been expanded to include saving the new customer's card details with Square, and setting up a subscription (see next page).

###Plans
In order to set up subscriptions, you need payment plans. In Square terms, these are one type of `CatalogObject`. This page allows you to create payment plans which can be used on the customers page.

### Orders
List and create orders. The one thing that this page doesn't do is take payments. It just allows you to create an order, which on its own probably isn't so useful. See the next page...

### Order & pay
Create an order, and have the customer pay for it.

## Acknowledgements
The basis of the code on the payment page is from [Alfred Zaki's GitHub repository](https://github.com/UpwardInfo/BlazorPay/tree/dev), which was the sole example I could find of anyone trying to use [Square's](https://squareup.com/) API from Blazor. I built on that (thanks Alfred) to produce this sample.

The code there is in the form of a sandalone component, which is meant for reuse. This repository shows my usage of that code, but integrating the component in the page where I was using it.

I made a couple of small changes to the original code...

- In order to support both sandbox and production, I moved the code that imports Square's JavaScript into the main code file. This means that I can use the same `Square.js` file in both modes.
- The sample usage code that Alfred showed (see [this issue](https://github.com/UpwardInfo/BlazorPay/issues/1)) referenced a `SquModels` class that wouldn't resolve for me. I had to change things like `new SquModels.Money` to just `new Money` to get it to compile.


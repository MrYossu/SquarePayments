# SquarePayments
## Sample code for working with the Square API from Blazor

The pages here show how to manage customers, take payments, etc (well, no etc just yet, but hopefully coming) in a Blazor app, using the Square API.

## Set up
In order to use this code, you need an account with Square (duh), and need to set three environment variables on your machine. See the home page of the app for details. In practice, you would use `appSettings.json`, Azure secrets or the like. I used environment variables so that I could commit the code without having to remember to blank out my credentials each time.

The basis of the code on the payment page is from [Alfred Zaki's GitHub repository](https://github.com/UpwardInfo/BlazorPay/tree/dev), which was the sole example I could find of anyone trying to use [Square's](https://squareup.com/) API from Blazor. I built on that (thanks Alfred) to produce this sample.

The code there is in the form of a sandalone component, which is meant for reuse. This repository shows my usage of that code, but integrating the component in the page where I was using it.

I made a couple of small changes to the original code...

- In order to support both sandbox and production, I moved the code that imports Square's JavaScript into the main code file. This means that I can use the same `Square.js` file in both modes.
- The sample usage code that Alfred showed (see [this issue](https://github.com/UpwardInfo/BlazorPay/issues/1)) referenced a `SquModels` class that wouldn't resolve for me. I had to change things like `new SquModels.Money` to just `new Money` to get it to compile.

## Caveat
The code here is just to show how it can be done. In practice, you would want to make some improvements. Obvious ones are...

- There is a noticeable delay before the Square controls appear on the page. We should show a busy indicator (or the like) while this is happening.
- Once you click the Pay button, there is little indication that anything is happening. Again, disabling th econtrols and showing a busy indicator would help.
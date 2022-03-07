# SquarePayments
## Sample code for taking payments with the Square API from Blazor

The basis of this code is from [this GitHub repository](https://github.com/UpwardInfo/BlazorPay/tree/dev), which was the sole example I could find of anyone trying to use [Square's](https://squareup.com/) API from Blazor.

The code there is in the form of a sandalone component, which is meant for reuse. This repository shows my usage of that code, but integrating the component in the page where I was using it.

I made a couple of small changes to the original code...

- In order to support both sandbox and production, I moved the code that imports Square's JavaScript into the main code file. This means that I can use the same `Square.js` file in both modes.
- Erm, I did smething else, but can't remember what it was now!

The code here is just to show how it can be done. In practice, you would want to make some improvements. Obvious ones are...

- There is a noticeable delay before the Square controls appear on the page. We should show a busy indicator (or the like) while this is happening.
- Once you click the Pay button, there is little indication that anything is happening. Again, disabling th econtrols and showing a busy indicator would help.
export async function addSquareCardPayment(element, appId, locationId) {
  const payments = Square.payments(appId, locationId);
  const card = await payments.card();
  await card.attach("#" + element);
  return card;
}

export async function getSquareCardToken(card) {
  const tokenResult = await card.tokenize();
  if (tokenResult.status === 'OK') {
    return tokenResult.token;
  } else {
    return null;
  }
}

export async function verifyTheBuyer(appId, locationId, token, amount, contact) {
  // amount: string of the amount, eg '1.00'
  // contact: details of the buyer, eg...
  /*
   {
      addressLines: ['123 Main Street', 'Apartment 1'],
      familyName: 'Doe',
      givenName: 'John',
      email: 'jondoe@gmail.com',
      country: 'GB',
      phone: '3214563987',
      region: 'LND',
      city: 'London',
    }
   */
  const payments = Square.payments(appId, locationId);
  const verificationResults = await payments.verifyBuyer(token, {
    amount: amount,
    billingContact: contact,
    currencyCode: 'GBP', // Hard-coded to GBP. You may need to change this
    intent: 'CHARGE'
  });
  return verificationResults.token;
}
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
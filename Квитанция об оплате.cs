@using System
@using System.Globalization
@using System.Linq
@using Resto.Front.PrintTemplates.Cheques.Razor
@using Resto.Front.PrintTemplates.Cheques.Razor.TemplateModels
@using System.Text.RegularExpressions

@inherits TemplateBase<IReceiptCheque>
@{
    var chequeTask = Model.ChequeTask;
    var order = Model.Order;
    var sum = chequeTask.Sales.Sum(sale => sale.GetCost() - sale.DiscountSum + sale.IncreaseSum).RoundMoney();

    var transliterationMap = new Dictionary <string, string> {
        {"а", "a"},
        {"б", "b"},
        {"в", "v"},
        {"г", "g"},
        {"д", "d"},
        {"е", "e"},
        {"ё", "e"},
        {"ж", "zh"},
        {"з", "z"},
        {"и", "i"},
        {"й", "y"},
        {"к", "k"},
        {"л", "l"},
        {"м", "m"},
        {"н", "n"},
        {"о", "o"},
        {"п", "p"},
        {"р", "r"},
        {"с", "s"},
        {"т", "t"},
        {"у", "y"},
        {"ф", "ph"},
        {"х", "h"},
        {"ц", "c"},
        {"ч", "ch"},
        {"ш", "sh"},
        {"щ", "sh"},
        {"ы", "i"},
        {"э", "e"},
        {"ю", "u"},
        {"я", "ya"},
        {"a", "a"},
        {"b", "b"},
        {"c", "c"},
        {"d", "d"},
        {"e", "e"},
        {"f", "f"},
        {"g", "g"},
        {"h", "h"},
        {"i", "i"},
        {"j", "j"},
        {"k", "k"},
        {"l", "l"},
        {"m", "m"},
        {"n", "n"},
        {"o", "o"},
        {"p", "p"},
        {"q", "q"},
        {"r", "r"},
        {"s", "s"},
        {"t", "t"},
        {"u", "u"},
        {"v", "v"},
        {"w", "w"},
        {"x", "x"},
        {"y", "y"},
        {"z", "z"}
        };

    var urls = new Dictionary <string, string> {
        {"1", "3&s"},
        {"2", "&c"},   
        {"3", "&en"},
        {"4", "XXXXXX"}, // код заведения
        {"5", "&tn"},
        {"6", "&wpid=XXXXXX"} // WPID
        };

    var codeMatches = Regex.Match(order.Waiter.GetNameOrEmpty(), ".*(\\d{6}).*");
    string code = null;
    bool codeFound = codeMatches.Groups.Count == 2;
    if (codeFound) { // means success
        code = codeMatches.Groups[1].Value;
        }

    var terminal = Model.CommonInfo.CurrentTerminal;
    var fullSum = order.GetFullSum() - order.DiscountItems.Where(di => !di.Type.PrintProductItemInPrecheque).Sum(di => di.GetDiscountSum());
 
    var transliteratedName = string.Concat(order.Waiter.GetNameOrEmpty().ToLower().Select(c => {
        string saveString;
    if (transliterationMap.TryGetValue(c.ToString(), out saveString)) {
        return saveString;
        } else {
            return "_";
            }
            }));

}

<doc>
    @if (!Model.ChequeInfo.IsForReport)
    {
        <left><split><whitespace-preserve>@Model.CommonInfo.CafeSetup.BillHeader</whitespace-preserve></split></left>
    }
    <whitespace-preserve>@Raw(string.Join(Environment.NewLine, Model.Extensions.BeforeCheque))</whitespace-preserve>

    @if (!Model.ChequeInfo.IsForReport)
    {
        <pair fit="left" left="@string.Format(Resources.CashRegisterFormat, Model.ChequeInfo.Session.CashRegisterNumber)" right="@Model.CommonInfo.Group.Name" />
        <pair left="@Resources.HeadCashRegisterShift" right="@Model.ChequeInfo.Session.Number" />
    }

    <center>@(chequeTask.IsStorno ? Resources.StornoCheque : chequeTask.IsBuy ? Resources.OrderBuyReceipt : Resources.OrderPaymentReceipt)</center>
    <pair left="@Resources.CurrentDate" right="@FormatLongDateTime(Model.ChequeInfo.OperationTime)" />
    <pair left="@string.Format(Resources.BillHeaderCashierPattern, chequeTask.CashierName)" right="@string.Format(Resources.BillHeaderOrderNumberPattern, order.Number)" />
    <left>@string.Format(Resources.BillHeaderWaiterPattern, order.Waiter.GetNameOrEmpty())</left>
    <left>@string.Format(Resources.BillHeaderWaiterPattern, @Regex.Replace(order.Waiter.GetNameOrEmpty(), "\\d{6}", ""))</left>
    @if (order.TabName != null)
    {
        <left>@string.Format(Resources.BillHeaderTabPattern, order.TabName)</left>
    }
    <left><split>@string.Format(Resources.BillHeaderLocationAndGuestsPattern, order.Table.Section.Name, order.Table.Number, order.Delivery != null ? order.Delivery.PersonCount : order.InitialGuestsCount)</split></left>

    @Sales(chequeTask)

    @if (chequeTask.ResultSum != sum)
    {
        <pair left="@Resources.BillFooterTotalPlain" right="@FormatMoney(sum)" />
    }

    @foreach (var discountItem in chequeTask.Discounts.Concat(chequeTask.Increases))
    {
        @DiscountIncrease(discountItem)
    }

    @if (chequeTask.PrintNds)
    {
        @Vats(chequeTask)
    }

    <pair left="@Resources.BillFooterTotal" right="@FormatMoney(chequeTask.ResultSum)" />

    @Payments(chequeTask)

    @if (!Model.ChequeInfo.IsForReport)
    {
        <center>@string.Format(Resources.AllSumsInFormat, Model.CommonInfo.CafeSetup.CurrencyName)</center>
        <np />
        <center><split><whitespace-preserve>@Model.CommonInfo.CafeSetup.BillFooter</whitespace-preserve></split></center>
    }

    @if (!chequeTask.IsStorno)
    {
        <np />
        <line />
        <center>@Resources.Signature</center>
    }
    <whitespace-preserve>@Raw(string.Join(Environment.NewLine, Model.Extensions.AfterCheque))</whitespace-preserve>

    @if (codeFound) {
        <f2><center>@("Чаевые нетмонет")</center></f2>
        <center>@("Наведите камеру на QR-код")</center>
        <qrcode size="small" correction="low">https://netmonet.co/tip/@code?o=@urls["1"]=@fullSum@urls["2"]=@order.Number@urls["5"]=@order.Table.Number@urls["6"]</qrcode>
        <center>@("или введите код на сайте netmonet.co")</center>
        <f1><center>@code</center></f1>
        } else {
            <f2><center>@("Чаевые нетмонет")</center></f2>
            <center>@("Наведите камеру на QR-код")</center>
            <qrcode size="small" correction="low">https://netmonet.co/tip/@urls["4"]?o=@urls["1"]=@fullSum@urls["2"]=@order.Number@urls["5"]=@order.Table.Number@urls["3"]=@transliteratedName</qrcode>
            <center>@("или введите код на сайте netmonet.co")</center>
            <f1><center>@urls["4"]</center></f1>
            }

</doc>

@helper Sales(IChequeTask chequeTask)
{
    <line />
    if (chequeTask.Sales.IsEmpty())
    {
        @Resources.ZeroChequeBody
        <line />
    }
    else if (Model.IsFullCheque)
    {
        <table>
            <columns>
                <column formatter="split" />
                <column align="right" autowidth="" />
                <column align="right" autowidth="" />
          </columns>
            <cells>
                @if (!Model.ChequeInfo.IsForReport)
                {
                  <ct>@Resources.NameTitle</ct>
                  <ct>@Resources.AmountShort</ct>
                  <ct>@Resources.ProductSum</ct>
                  <linecell />
                }
                @foreach (var sale in chequeTask.Sales)
                {
                    <c>@sale.Name</c>
                    if (sale.Amount != 1m)
                    {
                        <ct>@FormatAmount(sale.Amount)</ct>
                    }
                    else
                    {
                        <ct />
                    }
                    if (sale.GetCost() != 0m)
                    {
                        <ct>@FormatMoney(sale.GetCost())</ct>
                    }
                    else
                    {
                        <ct />
                    }
                    if (sale.IncreaseSum != 0m)
                    {
                        <c>@Resources.Increase</c>
                        <ct>@FormatPercent(sale.IncreasePercent)</ct>
                        <ct>@FormatMoney(sale.IncreaseSum)</ct>
                    }
                    if (sale.DiscountSum != 0m)
                    {
                        <c>@Resources.Discount</c>
                        <ct>@FormatPercent(-sale.DiscountPercent)</ct>
                        <ct>@FormatMoney(-sale.DiscountSum)</ct>
                    }
                }
          </cells>
        </table>
        <line />
    }
}

@helper DiscountIncrease(IChequeTaskDiscountItem discountItem)
{
    <table>
        <columns>
            <column formatter="split" />
            <column />
            <column align="right" autowidth="" />
        </columns>
        <cells>
            <c>@discountItem.Name</c>
            <c>@FormatPercent(discountItem.Percent)</c>
            <ct>@FormatMoney(discountItem.Sum)</ct>
        </cells>
    </table>
    if (!string.IsNullOrWhiteSpace(discountItem.CardNumber))
    {
        @:@string.Format(Resources.CardPattern, discountItem.CardNumber)
    }
}

@helper Vats(IChequeTask chequeTask)
{
    var vats = chequeTask.Sales
        .GroupBy(sale => sale.NdsPercent)
        .Where(group => group.Key > 0m)
        .Select(group => Tuple.Create(group.Key, group.Sum(sale => (sale.ResultSum - sale.ResultSum / (1m + @group.Key / 100m)).RoundMoneyFractional())))
        .ToList();

    var vatSum = vats.Sum(tuple => tuple.Item2);

    if (vatSum != 0)
    {
        <pair left="@Resources.ResultSumWithoutNds" right="@FormatMoneyFractional(chequeTask.ResultSum - vatSum)" />
        foreach (var percentAndSum in vats)
        {
            <pair left="@string.Format(Resources.VatFormat, FormatPercent(percentAndSum.Item1))" right="@FormatMoneyFractional(percentAndSum.Item2)" />
        }
        if (vats.Count > 1)
        {
            <pair left="@Resources.VatSum" right="@FormatMoneyFractional(vatSum)" />
        }
    }
}

@helper Payments(IChequeTask chequeTask)
{
    <line />
    foreach (var prepayItem in chequeTask.Prepayments)
    {
        var paymentCurrencyIsoName = prepayItem.CurrencyName;
        if (!string.IsNullOrWhiteSpace(paymentCurrencyIsoName))
        {
            <pair left="@string.Format(Resources.PrepayTemplate, prepayItem.PaymentTypeName)"
                  right="@string.Format(Resources.CurrencyFormat, paymentCurrencyIsoName, FormatMoney(prepayItem.SumInCurrency, paymentCurrencyIsoName))" />
        }
        else
        {
            <pair left="@string.Format(Resources.PrepayTemplate, prepayItem.PaymentTypeName)"
                  right="@FormatMoney(prepayItem.Sum)" />
        }

        if (!string.IsNullOrWhiteSpace(prepayItem.Comment))
        {
            <left>@prepayItem.Comment</left>
        }
    }

    if (!chequeTask.CashPayments.IsEmpty() || chequeTask.CardPayments.IsEmpty() && chequeTask.AdditionalPayments.IsEmpty())
    {
        var hasMultiCurrencyPayment = chequeTask.CashPayments.Any(p => !string.IsNullOrWhiteSpace(p.CurrencyName));
        if (hasMultiCurrencyPayment && !chequeTask.IsStorno)
        {
            foreach (var cashPaymentItem in chequeTask.CashPayments)
            {
                var defaultCurrencyIsoName = Model.CommonInfo.CafeSetup.CurrencyIsoName;
                var paymentCurrencyIsoName = cashPaymentItem.CurrencyName;

                if (!string.IsNullOrWhiteSpace(paymentCurrencyIsoName))
                {
                    <pair left="@cashPaymentItem.PaymentTypeName"
                          right="@string.Format(Resources.CurrencyFormat, paymentCurrencyIsoName, FormatMoney(SetSign(cashPaymentItem.SumInCurrency), paymentCurrencyIsoName))" />

                    var currency = Model.Order.FixedCurrencyRates.Keys.SingleOrDefault(r => r.IsoName.Equals(paymentCurrencyIsoName));
                    if (currency != null)
                    {
                        var rate = Model.Order.FixedCurrencyRates[currency];
                        <right>@string.Format(Resources.CurrencyRateFormat, currency.IsoName, rate.ToString("f4", CultureInfo.CurrentCulture), defaultCurrencyIsoName)</right>
                    }
                }
                else
                {
                    <pair left="@cashPaymentItem.PaymentTypeName"
                          right="@string.Format(Resources.CurrencyFormat, defaultCurrencyIsoName, FormatMoney(SetSign(cashPaymentItem.Sum)))" />
                }

                if (!string.IsNullOrWhiteSpace(cashPaymentItem.Comment))
                {
                    <left>@cashPaymentItem.Comment</left>
                }
            }
        }
        else
        {
            @Payment(Resources.Cash, chequeTask.CashPayment)
        }
    }
    foreach (var cardPaymentItem in chequeTask.CardPayments)
    {
        @Payment(string.Format(Resources.CardPattern, cardPaymentItem.PaymentTypeName), cardPaymentItem.Sum, cardPaymentItem.Comment)
    }
    foreach (var additionalPaymentItem in chequeTask.AdditionalPayments)
    {
        @Payment(additionalPaymentItem.PaymentTypeName, additionalPaymentItem.Sum, additionalPaymentItem.Comment)
    }

    var orderPaymentsSumWithoutPrepay = chequeTask.CashPayment + chequeTask.CardPayments.Sum(p => p.Sum) + chequeTask.AdditionalPayments.Sum(p => p.Sum);
    var orderPaymentsSumWithPrepay = orderPaymentsSumWithoutPrepay + SetSign(chequeTask.Prepayments.Sum(p => p.Sum));
    var changeSum = Math.Max(orderPaymentsSumWithPrepay - chequeTask.ResultSum, 0m);

    var hasMultiplePayments = (chequeTask.CashPayment != 0m ? 1 : 0) +
                               chequeTask.CardPayments.Count() +
                               chequeTask.AdditionalPayments.Count() +
                               chequeTask.Prepayments.Count() > 1;

    if (hasMultiplePayments)
    {
        <pair left="@Resources.Total" right="@FormatMoney(SetSign(orderPaymentsSumWithoutPrepay))" />
    }
    if (changeSum > 0m)
    {
        <pair left="@Resources.Change" right="@FormatMoney(changeSum)" />
    }

    if (Model.Order.Donations.Any())
    {
        <line/>
        var hasMultiCurrencyDonation = Model.Order.Donations.Any(d => d.CurrencyInfo != null);
        foreach (var donation in Model.Order.Donations)
        {
            if (!hasMultiCurrencyDonation)
            {
                <pair left="@string.Format(Resources.DonationsPattern, donation.Type.Name, donation.DonationType.Name)"
                      right="@FormatMoney(donation.Sum)"/>
            }
            else
            {
                var defaultCurrencyIsoName = Model.CommonInfo.CafeSetup.CurrencyIsoName;

                if (donation.CurrencyInfo != null)
                {
                    var donationCurrencyIsoName = donation.CurrencyInfo.Currency.IsoName;

                    <pair left="@string.Format(Resources.DonationsPattern, donation.Type.Name, donation.DonationType.Name)"
                          right="@string.Format(Resources.CurrencyFormat, donationCurrencyIsoName, FormatMoney(donation.CurrencyInfo.Sum, donationCurrencyIsoName))" />

                    var currency = Model.Order.FixedCurrencyRates.Keys.SingleOrDefault(r => r.IsoName.Equals(donationCurrencyIsoName));
                    if (currency != null)
                    {
                        var rate = Model.Order.FixedCurrencyRates[currency];
                        <right>@string.Format(Resources.CurrencyRateFormat, currency.IsoName, rate.ToString("f4", CultureInfo.CurrentCulture), defaultCurrencyIsoName)</right>
                    }
                }
                else
                {
                    <pair left="@string.Format(Resources.DonationsPattern, donation.Type.Name, donation.DonationType.Name)"
                          right="@string.Format(Resources.CurrencyFormat, defaultCurrencyIsoName, FormatMoney(donation.Sum))" />
                }
            }
        }
    }
}

@helper Payment(string name, decimal sum, string comment = null)
{
    <pair left="@name" right="@FormatMoney(SetSign(sum))" />
    if (!string.IsNullOrWhiteSpace(comment))
    {
        <left>@comment</left>
    }
}

@functions
{
    private decimal SetSign(decimal sum)
    {
        return Model.ChequeTask.IsStorno ? -sum : sum;
    }
}

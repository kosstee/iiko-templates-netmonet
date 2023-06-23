@using System.Collections.Generic
@using System.Linq
@using Resto.Front.PrintTemplates.Cheques.Razor
@using Resto.Front.PrintTemplates.Cheques.Razor.TemplateModels
@using Resto.Front.PrintTemplates.RmsEntityWrappers
@using System.Text.RegularExpressions

@inherits TemplateBase<ICashMemoCheque>
@{
    var order = Model.Order;
    var chequeInfo = Model.ChequeInfo;
    var session = chequeInfo.Session;
    var notDeletedItems = order.Guests.SelectMany(guest => guest.Items).Where(item => item.DeletionInfo == null).ToList();
    var fullSum = order.GetFullSum();
    var subTotal = fullSum - order.DiscountItems.Where(discountItem => discountItem.IsCategorized && discountItem.PrintDetailedInPrecheque).Sum(discountItem => discountItem.GetDiscountSum());
    var vatSum = order.GetVatSumExcludedFromPrice();
    var resultSum = subTotal - order.DiscountItems.Where(discountItem => !discountItem.IsCategorized || !discountItem.PrintDetailedInPrecheque).Sum(discountItem => discountItem.GetDiscountSum()) + vatSum;
    var changeSum = order.Payments.Sum(p => p.Sum) + order.GetPrepaySum() - resultSum;
    var discountItems = order.DiscountItems.ToList();
    List<IOrderPaymentTransaction> transactions = null;

    var groupName = Model.CommonInfo.Group.Name;
    var transliterationMap = new Dictionary<string, string> {
        {"а", "a"},  {"б", "b"},  {"в", "v"},  {"г", "g"}, {"д", "d"},
        {"е", "e"},  {"ё", "e"},  {"ж", "zh"}, {"з", "z"}, {"и", "i"},
        {"й", "y"},  {"к", "k"},  {"л", "l"},  {"м", "m"}, {"н", "n"},
        {"о", "o"},  {"п", "p"},  {"р", "r"},  {"с", "s"}, {"т", "t"},
        {"у", "y"},  {"ф", "ph"}, {"х", "h"},  {"ц", "c"}, {"ч", "ch"},
        {"ш", "sh"}, {"щ", "sh"}, {"ы", "i"},  {"э", "e"}, {"ю", "u"},
        {"я", "ya"}, {"a", "a"},  {"b", "b"},  {"c", "c"}, {"d", "d"},
        {"e", "e"},  {"f", "f"},  {"g", "g"},  {"h", "h"}, {"i", "i"},
        {"j", "j"},  {"k", "k"},  {"l", "l"},  {"m", "m"}, {"n", "n"},
        {"o", "o"},  {"p", "p"},  {"q", "q"},  {"r", "r"}, {"s", "s"},
        {"t", "t"},  {"u", "u"},  {"v", "v"},  {"w", "w"}, {"x", "x"},
        {"y", "y"},  {"z", "z"}
        };

    var codeMatches = Regex.Match(chequeInfo.Cashier.GetNameOrEmpty(), ".*(\\d{6}).*");
    string code = null;
    bool codeFound = codeMatches.Groups.Count == 2;
    if (codeFound) {
        code = codeMatches.Groups[1].Value;
        }

    var transliteratedName = string.Concat(chequeInfo.Cashier.GetNameOrEmpty().ToLower().Select(c => {
        string saveString;
    if (transliterationMap.TryGetValue(c.ToString(), out saveString)) {
        return saveString;
        } else {
            return "_";
            }
            }));

    var urls = new Dictionary<string, string> {
        {"source", "?o=3"},
        {"sum", string.Concat("&s=", fullSum)},
        {"number", string.Concat("&c=", order.Number)},
        {"table", string.Concat("&tn=", order.Table.Number)},
        {"name", string.Concat("&en=", transliteratedName)},
        {"wp", "&wpid="}
        };
    
    /*  _  _ _/__ _  _  _  _ _/_ */
    /* / //_'/ / / //_// //_'/   */
    var fallbackCode = "XXXXXX"; // код заведения
    var wpid = "XXXXXXX";        // WPID
                                 //
    bool useGroupCode = false;   // true для группового кода
    bool useGrouping = false;    // true для разделения по группам
    
    // groupDictionary заполняется только при useGrouping = true
    // указать имя группы вместо "Группа 1", "Группа 2" и тд.
    var groupDictionary = new Dictionary<string, Tuple<string, string, bool>> {        
        {"Группа 1", Tuple.Create(
            "XXXXXX",    // код заведения
            "XXXXXXX",   // WPID
            false        // true для группового кода
            )},
        {"Группа 2", Tuple.Create(
            "XXXXXX",    // код заведения
            "XXXXXXX",   // WPID
            false        // true для группового кода
            )}
        };

    var url = "https://netmonet.co/tip/";    
    var personalUrlParams = string.Concat(urls["source"], urls["sum"], urls["number"], urls["table"]);
    var fallbackUrlParams = string.Concat(personalUrlParams, urls["name"]);

    if (useGrouping && groupDictionary.ContainsKey(groupName)) {
        fallbackCode = groupDictionary[groupName].Item1;
        wpid = groupDictionary[groupName].Item2;
        useGroupCode = groupDictionary[groupName].Item3;
        }
        
    if (useGroupCode && !codeFound) {
        url = string.Concat(url, "group/");
        }
    
    if (order.CloseInfo != null)
    {
        discountItems.AddRange(order.CloseInfo.DiscountsForPaymentsAsDiscount);
        transactions = order.CloseInfo.PaymentTransactions.Where(t => !t.IsPrepayClosedTransaction).ToList();
        resultSum -= transactions.Where(t => t.PaymentType.ProccessAsDiscount).Sum(t => t.Sum);
        changeSum = 0;
    }

    var paymentsData = (transactions != null
            ? transactions.Where(t => !t.PaymentType.ProccessAsDiscount).Select(t => new { t.PaymentType, t.Sum })
            : order.Payments.Select(p => new { PaymentType = p.Type, p.Sum }))
        .ToList();

    var payments = paymentsData
        .Where(p => p.PaymentType.Group != PaymentGroup.Cash)
        .Select(p => new { p.PaymentType.Name, p.Sum })
        .StartWith(new { Name = Resources.Cash, Sum = paymentsData.Where(p => p.PaymentType.Group == PaymentGroup.Cash).Sum(p => p.Sum) - changeSum })
        .ToList();

    var categorizedDiscountItems = new List<IDiscountItem>();
    var nonCategorizedDiscountItems = new List<IDiscountItem>();

    foreach (var discountItem in discountItems)
    {
        if (discountItem.IsCategorized && discountItem.PrintDetailedInPrecheque)
        {
            categorizedDiscountItems.Add(discountItem);
        }
        else
        {
            nonCategorizedDiscountItems.Add(discountItem);
        }
    }
}

<doc>
    @* header *@
    <left><split><whitespace-preserve>@Model.CommonInfo.CafeSetup.BillHeader</whitespace-preserve></split></left>
    <center>@Resources.CashMemoChequeTitle</center>
    <pair fit="left" left="@string.Format(Resources.CashRegisterFormat, session.CashRegisterNumber)" right="@Model.CommonInfo.Group.Name" />
    <pair left="@Resources.HeadCashRegisterShift" right="@session.Number" />
    <pair left="@Resources.CafeSessionOpenDate" right="@FormatLongDateTime(session.OpenTime)" />
    <pair left="@Resources.CurrentDate" right="@FormatLongDateTime(chequeInfo.OperationTime)" />
    <pair left="@string.Format(Resources.BillHeaderCashierPattern, chequeInfo.Cashier.GetNameOrEmpty())" right="@string.Format(Resources.BillHeaderOrderNumberPattern, order.Number)" />
    <pair left="@string.Format(Resources.BillHeaderCashierPattern, @Regex.Replace(chequeInfo.Cashier.GetNameOrEmpty(), "\\d{6}", ""))" right="@string.Format(Resources.BillHeaderOrderNumberPattern, order.Number)" />
  
    @* body *@
    <line />
    @Products(notDeletedItems, categorizedDiscountItems, order.Table.Section.PrintProductItemCommentInCheque)
    <line />
    
    @if (nonCategorizedDiscountItems.Count > 0)
    {
        <pair left="@Resources.BillFooterTotalPlain" right="@FormatMoney(subTotal)" />
        @NonCategorizedDiscounts(nonCategorizedDiscountItems, fullSum)
    }

    @if (vatSum != 0m)
    {
        var vatSumsByVat = order.GetIncludedEntries()
            .Where(orderEntry => !orderEntry.VatIncludedInPrice)
            .GroupBy(orderEntry => orderEntry.Vat)
            .Where(group => group.Key != 0m)
            .Select(group => new { Vat = group.Key, Sum = group.Sum(orderEntry => orderEntry.ExcludedVat) })
            .ToList();

        foreach (var vatWithSum in vatSumsByVat)
        {
            <pair left="@string.Format(Resources.VatFormat, vatWithSum.Vat)" right="@FormatMoney(vatWithSum.Sum)" />
        }
        if (vatSumsByVat.Count > 1)
        {
            <pair left="@Resources.VatSum" right="@FormatMoney(vatSum)" />
        }
    }
  
    @* payments *@
    <pair left="@Resources.BillFooterTotalLower" right="@FormatMoney(resultSum)" />
    <line />
    
    @foreach (var prepay in order.PrePayments)
    {
        <pair left="@string.Format(Resources.PrepayTemplate, prepay.Type.Name)" right="@FormatMoney(prepay.Sum)" />
    }
    @foreach (var payment in payments)
    {
        <pair left="@payment.Name" right="@FormatMoney(payment.Sum)" />
    }
    <line />
    @string.Format(Resources.FooterTotalUpper, FormatMoney(resultSum))

    @* footer *@
    <pair left="@Resources.ItemsCount" right="@itemsCount" />
    <left>@string.Format(Resources.SumFormat, FormatMoneyInWords(resultSum))</left>
    <np />
    <left>@Resources.Released</left>
    <fill symbols="_"><np /></fill>
    <center>@Resources.MemoChequeSignature</center>
    <np />
    <center>@string.Format(Resources.AllSumsInFormat, Model.CommonInfo.CafeSetup.CurrencyName)</center>
    <np />
        @if (!useGrouping || groupDictionary.ContainsKey(groupName)) {
        if (codeFound) {
            <f2><center>@("Отзывы и чаевые")</center></f2>
            <f2><center>@("нетмонет")</center></f2>
            <qrcode size="small" correction="low">@url@code@personalUrlParams@urls["wp"]@wpid</qrcode>
            <center>@("Наведите камеру на QR-код")</center>
            <center>@("или введите " + @code + " на netmonet.co")</center>
            } else {
                <f2><center>@("Отзывы и чаевые")</center></f2>
                <f2><center>@("нетмонет")</center></f2>
                <qrcode size="small" correction="low">@url@fallbackCode@fallbackUrlParams@urls["wp"]@wpid</qrcode>
                <center>@("Наведите камеру на QR-код")</center>
                <center>@("или введите " + @fallbackCode + " на netmonet.co")</center>
                }
        }
    <center><split><whitespace-preserve>@Model.CommonInfo.CafeSetup.BillFooter</whitespace-preserve></split></center>
</doc>

@helper Products(List<IOrderItem> orderItems, List<IDiscountItem> categorizedDiscountItems, bool showComments)
{
    if (orderItems.Count == 0)
    {
        @Resources.ZeroChequeBody
    }
    else
    {
        <table>
            <columns>
                <column formatter="split" />
                <column align="right" autowidth="" />
                <column align="right" autowidth="" />
                <column align="right" autowidth="" />
            </columns>
            <cells>
                <ct>@Resources.NameColumnHeader</ct>
                <ct>@Resources.AmountShort</ct>
                <ct>@Resources.ProductPrice</ct>
                <ct>@Resources.ResultSum</ct>
                <linecell />
                @foreach (var orderItem in orderItems)
                {
                    @Product(orderItem, categorizedDiscountItems, showComments)
                }
          </cells>
        </table>
    }
}

@helper Product(IOrderItem orderItem, List<IDiscountItem> categorizedDiscountItems, bool showComments)
{
    itemsCount++;
    var productItem = orderItem as IProductItem;
    if (productItem != null && productItem.CompoundsInfo != null && productItem.CompoundsInfo.IsPrimaryComponent)
    {
        <c colspan="4">@string.Format("{0} {1}", productItem.CompoundsInfo.ModifierSchemaName, productItem.ProductSize == null ? string.Empty : productItem.ProductSize.Name)</c>
    
        // для разделенной пиццы комменты печатаем под схемой
        if (showComments && productItem.Comment != null && !productItem.Comment.Deleted)
        {
            <c colspan="4">
                <table cellspacing="0">
                    <columns>
                        <column width="2" />
                        <column />
                    </columns>
                    <cells>
                        <c />
                        <c><split>@productItem.Comment.Text</split></c>
                    </cells>
                </table>
            </c>
        }
    
        // у пиццы не может быть удаленных модификаторов, поэтому берем весь список
        foreach (var orderEntry in productItem.ModifierEntries.Where(orderEntry => ModifiersFilter(orderEntry, productItem, true)))
        {
            itemsCount++;
            var productName = orderEntry.ProductCustomName ?? orderEntry.Product.Name;
            <c colspan="4"><whitespace-preserve>@("  " + productName)</whitespace-preserve></c>

            <c colspan="2"><right>@string.Format("{0} {1} x", FormatAmount(orderEntry.Amount), orderEntry.Product.MeasuringUnit.Name)</right></c>
            <ct>@FormatMoney(orderEntry.Price)</ct>
            <ct>@FormatMoney(orderEntry.Cost)</ct>
            @CategorizedDiscounts(orderEntry, categorizedDiscountItems)
        }
    }
    
    if (productItem != null && productItem.CompoundsInfo != null)
    {
        <c colspan="4"><whitespace-preserve>@(" 1/2 " + GetOrderEntryNameWithProductSize(productItem))</whitespace-preserve></c>
    }
    else
    {
        <c colspan="4">@GetOrderEntryNameWithProductSize(orderItem)</c>
    }

    <c colspan="2"><right>@string.Format("{0} {1} x", FormatAmount(orderItem.Amount), orderItem.Product.MeasuringUnit.Name)</right></c>
    <ct>@FormatMoney(orderItem.Price)</ct>
    <ct>@FormatMoney(orderItem.Cost)</ct>

    // здесь комменты для обычных блюд и целых пицц
    if (showComments && productItem != null && productItem.Comment != null && !productItem.Comment.Deleted && productItem.CompoundsInfo == null)
    {
        <c colspan="4">
            <table cellspacing="0">
                <columns>
                    <column width="2" />
                    <column />
                </columns>
                <cells>
                    <c />
                    <c><split>@productItem.Comment.Text</split></c>
                </cells>
            </table>
        </c>
    }

    @CategorizedDiscounts(orderItem, categorizedDiscountItems)
    foreach (var child in GetFilteredChildren(orderItem))
    {
        itemsCount++;
        var productName = child.ProductCustomName ?? child.Product.Name;
        <c colspan="4"><whitespace-preserve>@string.Format("  {0}", productName)</whitespace-preserve></c>
        
        <c colspan="2"><right>@string.Format("{0} {1} x", FormatAmount(child.Amount), child.Product.MeasuringUnit.Name)</right></c>
        <ct>@FormatMoney(child.Price)</ct>
        <ct>@FormatMoney(child.Cost)</ct>
        @CategorizedDiscounts(child, categorizedDiscountItems)
    }
}

@helper CategorizedDiscounts(IOrderEntry orderEntry, IEnumerable<IDiscountItem> categorizedDiscountItems)
{
    foreach (var discountItem in categorizedDiscountItems.Where(d => d.DiscountSums.ContainsKey(orderEntry)))
    {
        if (discountItem.Type.DiscountBySum)
        {
            <c colspan="3"><whitespace-preserve>@string.Format("  {0}", discountItem.Type.PrintableName)</whitespace-preserve></c>
        }
        else
        {
            <c colspan="3"><whitespace-preserve>@string.Format(Resources.CashMemoChequeDiscountItemNameAndPercentFormat, discountItem.Type.PrintableName,
                FormatPercent(-CalculatePercent(orderEntry.Cost, discountItem.GetDiscountSumFor(orderEntry))))</whitespace-preserve>
            </c>
        }
        <ct>@FormatMoney(-discountItem.GetDiscountSumFor(orderEntry))</ct>
        if (discountItem.CardInfo != null && !string.IsNullOrWhiteSpace(discountItem.CardInfo.MaskedCard))
        {
            <c colspan="4">@string.Format(Resources.CardPattern, discountItem.CardInfo.MaskedCard)</c>
        }
    }
}

@helper NonCategorizedDiscounts(IEnumerable<IDiscountItem> discountItems, decimal fullSum)
{
    <table>
        <columns>
            <column formatter="split" />
            <column align="right" autowidth="" />
            <column align="right" autowidth="" />
        </columns>
        <cells>
            @foreach (var discountItem in discountItems)
            {
                if (discountItem.Type.DiscountBySum)
                {
                    <c colspan="2">@discountItem.Type.PrintableName</c>
                }
                else
                {
                    <ct>@discountItem.Type.PrintableName</ct>
                    <ct>@FormatPercent(-CalculatePercent(fullSum, discountItem.GetDiscountSum()))</ct>
                }
                <ct>@FormatMoney(-discountItem.GetDiscountSum())</ct>
                if (discountItem.CardInfo != null && !string.IsNullOrWhiteSpace(discountItem.CardInfo.MaskedCard))
                {
                    <c colspan="3">@string.Format(Resources.CardPattern, discountItem.CardInfo.MaskedCard)</c>
                }
            }
        </cells>
    </table>
}

@functions
{
    private int itemsCount;

    private static IEnumerable<IOrderEntry> GetFilteredChildren(IOrderItem orderItem)
    {
        var productItem = orderItem as IProductItem;
        if (productItem != null)
            return productItem.ModifierEntries.Where(m => ModifiersFilter(m, productItem));

        return ((ITimePayServiceItem)orderItem).RateScheduleEntries;
    }

    private static bool CommonModifiersFilter(bool isCommonModifier, IProductItem parent, bool onlyCommonModifiers)
    {
        if (parent.CompoundsInfo != null && parent.CompoundsInfo.IsPrimaryComponent)
        {
            if (onlyCommonModifiers && !isCommonModifier)
                return false;
            if (!onlyCommonModifiers && isCommonModifier)
                return false;
            return true;
        }
        return true;
    }

    private static bool ModifiersFilter(IModifierEntry modifierEntry, IProductItem parent, bool onlyCommonModifiers = false)
    {
        if (!CommonModifiersFilter((modifierEntry).IsCommonModifier, parent, onlyCommonModifiers))
            return false;

        if (modifierEntry.DeletionInfo != null)
            return false;

        if (modifierEntry.Cost > 0m)
            return true;

        if (modifierEntry.ChildModifier == null)
            return true;

        if (!modifierEntry.ChildModifier.HideIfDefaultAmount)
            return true;

        var amountPerItem = modifierEntry.ChildModifier.AmountIndependentOfParentAmount
            ? modifierEntry.Amount
            : modifierEntry.Amount / GetParentAmount(parent, onlyCommonModifiers);

        return amountPerItem != modifierEntry.ChildModifier.DefaultAmount;
    }

    private static string GetOrderEntryNameWithProductSize(IOrderEntry orderEntry)
    {
        var productName = orderEntry.ProductCustomName ?? orderEntry.Product.Name;
        var productItem = orderEntry as IProductItem;
        return productItem == null || productItem.ProductSize == null || productItem.CompoundsInfo != null
          ? productName
          : string.Format(Resources.ProductNameWithSizeFormat, productName, productItem.ProductSize.Name);
    }

    private static decimal GetParentAmount(IOrderItem parent, bool onlyCommonModifiers)
    {
        return onlyCommonModifiers ? parent.Amount * 2 : parent.Amount;
    }
}
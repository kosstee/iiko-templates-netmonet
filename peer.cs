@*
  V 1.1

  Данный шаблон пречека поддерживает работу с несколькими кодами нетмонет на одном сервере,
  разделение по контрагентам для всего заказа,
  разделение по контрагентам для каждого гостя.
  В данном шаблоне реализована возможность отключения печати кода нетмонет для всех должностей сотрудников кроме заданной.
  Можно включить и отключить только нужные функции задав нужные значения переменным.  

  enabledNetMonet - Нетмонет
  enabledSeparationByCountr - Разделение по контрагентам
  enabledSeparationByCountrForEveryGuest - Разделение по контрагентам для каждого гостя

  Значения переменных может быть true или false;

  Для работы с несколькими кодами Нетмонет необходимо добавить в словарь Restaurants элемент
  {"Имя группы", "XXXXXX"},
  Где:
  Имя группы - имя группы заведения, для которого необходимо установить отдельный код Нетмонет,
  XXXXXX - код Нетмонет.

  По умолчанию используется Стандартный код.
*@

@using System
@using System.Collections.Generic
@using System.Linq
@using Resto.Front.PrintTemplates.Cheques.Razor
@using Resto.Front.PrintTemplates.Cheques.Razor.TemplateModels
@using Resto.Front.PrintTemplates.RmsEntityWrappers
@using System.Text.RegularExpressions

@inherits TemplateBase<IBillCheque>

@{
  enabledNetMonet = false;
  enabledSeparationByCountr = false;
  enabledSeparationByCountrForEveryGuest = false;

  var Waiters = new List<string>
  {
    "1 Элина",
    "6 Валера",
    "2 Анастасия",
    "10 Евгений",
    "11 Демьян",
    "12 Карина",
    "2 Дима",
    "3 Катя",
    "5 Алия",
    "8 Андрей",
    "13 Марат",
  };

  if(Waiters.Contains( Model.CommonInfo.CurrentUser.Name )) 
  {
    enabledNetMonet = true;
  }

  var Restaurants = new Dictionary<string, string>
  {
    {"Стандартный код", "280054"}, // ПОМЕНЯТЬ XXXXXX НА КОД, ПРИСЛАННЫЙ ВМЕСТЕ С ИНСТРУКЦИЕЙ
  };

  var order = Model.Order;
  var transliterationMap = new Dictionary<string, string>
  {
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

  var urls = new Dictionary<string, string>
  {
    {"1", "3&s"},
    {"2", "&c"},   
    {"3", "&en"},
    {"5", "&tn"},
  };

  string currentGroup = Model.CommonInfo.Group.Name;
  string currentCode = Restaurants.ContainsKey(currentGroup) ? Restaurants[currentGroup] : Restaurants["Стандартный код"];

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
  @if (Model.AdditionalServiceChequeInfo == null)
  {
    <center><f1>
      @Resources.BillHeaderTitle
    </f1></center>
  }
  <pair fit="right" 
        left="@string.Format(Resources.BillHeaderWaiterPattern, order.Waiter.GetNameOrEmpty())"
        right="@string.Format(Resources.BillHeaderTablePattern, order.Table.Number)" />

  <pair fit="right"
        left="@string.Format(Resources.BillHeaderOrderOpenPattern, FormatLongDateTime(order.OpenTime))"
        right="@string.Format(Resources.BillHeaderOrderNumberPattern, order.Number)" />

  <pair fit="right"
        left='@string.Format("Пречек: {0}", FormatLongDateTime(Model.CommonInfo.CurrentTime))'
        right="@string.Format(Resources.BillHeaderSectionPattern, order.Table.Section.Name)" />

  @if (Model.AdditionalServiceChequeInfo != null)
  {
    <left>
      <left>@string.Format(Resources.BillHeaderWaiterPattern, @Regex.Replace(order.Waiter.GetNameOrEmpty(), "\\d{6}", ""))</left>
    </left>
  }

  @foreach (var clientInfo in
    from discountItem in order.DiscountItems
    where discountItem.CardInfo != null
    select discountItem.CardInfo into cardInfo
    select string.IsNullOrWhiteSpace(cardInfo.MaskedCard) ? cardInfo.Owner : string.Format("{0} ({1})", cardInfo.Owner, cardInfo.MaskedCard) into clientInfo
    where !string.IsNullOrWhiteSpace(clientInfo)
    select clientInfo)
  {
    <left><split>
      @string.Format(Resources.ClientFormat, clientInfo)
    </split></left>
  }
  <whitespace-preserve>@Raw(string.Join(Environment.NewLine, Model.Extensions.AfterHeader))</whitespace-preserve>
  @if (Model.AdditionalServiceChequeInfo != null)
  {
    if (order.ClientBinding != null && !string.IsNullOrWhiteSpace(order.ClientBinding.CardNumber))
    {
      <left><split>
        @string.Format(Resources.CardPattern, order.ClientBinding.CardNumber)
      </split></left>
    }
    <np />
    <center>
      @Resources.AdditionalServiceHeaderTitle
    </center>
  }
  @* Header (end) *@

  @* Body (begin) *@
  <table>
    <columns>
      <column formatter="split"/>
      <column align="right" autowidth="" />
      <column align="right" autowidth="" />
    </columns>
    <cells>
      @Guests()
      
      <linecell />
      
      @Summaries()

    </cells>
  </table>
  @* Body (end) *@

  @* Footer (begin) *@
  @if(enabledNetMonet)
  {
    if (codeFound)
    {
      <np />
      <f1>
        <center>@("Безналичные чаевые")</center>
      </f1>
      <f0>
        <np />	
        <qrcode size="normal" correction="low">https://netmonet.co/tip/@code?o=@urls["1"]=@fullSum@urls["2"]=@order.Number@urls["5"]=@order.Table.Number</qrcode>              		       
      </f0>	
    }
    else
    {
      <np />
      <f1>
        <center>@("Безналичные чаевые")</center>
      </f1>
      <f0>
        <np />
        <qrcode size="normal" correction="low">https://netmonet.co/tip/@currentCode?o=@urls["1"]=@fullSum@urls["2"]=@order.Number@urls["5"]=@order.Table.Number@urls["3"]=@transliteratedName</qrcode>       
      </f0>
    }
  }  
  <np />
  <whitespace-preserve>@Raw(string.Join(Environment.NewLine, Model.Extensions.BeforeFooter))</whitespace-preserve>
  <center>
    <split>
      <whitespace-preserve>@Model.CommonInfo.CafeSetup.BillFooter</whitespace-preserve>
    </split>
  </center>
  <np />
  <np />
    <whitespace-preserve>@Raw(string.Join(Environment.NewLine, Model.Extensions.AfterFooter))</whitespace-preserve>
  <np />
  @* Footer (end) *@
</doc>

@helper Guests()
{
  var order = Model.Order;
  Func<IOrderItem, bool> orderItemsFilter;
  if (Model.AdditionalServiceChequeInfo != null)
  {
    orderItemsFilter = orderItem => Model.AdditionalServiceChequeInfo.AddedOrderItems.Contains(orderItem);
  }
  else
  {
    orderItemsFilter = orderItem => orderItem.DeletionInfo == null;
  }

  var guestsWithItems = order.Guests
    .Select(guest => new { Guest = guest, Items = guest.Items.Where(item => orderItemsFilter(item) && OrderItemsToPrintFilter(item, order.DiscountItems)) })
    .Where(guestWithItems => guestWithItems.Items.Any())
    .ToList();
    
  
  if (guestsWithItems.Any())
  {
    <linecell />
    <ct>
      @Resources.NameColumnHeader
    </ct>
    <ct>
      @Resources.ProductAmount
    </ct>
    <ct>
      @Resources.ResultSum
    </ct>
    <linecell />
  }

  if (guestsWithItems.Count == 1)
  {
    @SingleGuest(guestsWithItems.Single().Items)
  }
  else
  {
    @OneOfMultipleGuests(guestsWithItems.First().Guest, guestsWithItems.First().Items)

    foreach (var guestWithItems in guestsWithItems.Skip(1))
    {
      <linecell symbols=" " />
      @OneOfMultipleGuests(guestWithItems.Guest, guestWithItems.Items)
    }
  }      
}

@helper SeparationByContr(IEnumerable<IOrderItem> items)
{
  <c colspan="0">По контрагентам:</c>

  decimal ooo = 0m;
  decimal ip = 0m;

  foreach(var productItem in items.Where(item => item.DeletionInfo == null))
  {
    @*<c colspan="0">@productItem.Product.Name </c>*@
    
    var productItemSum = productItem.Cost - Model.Order.DiscountItems.Select(di => di.GetDiscountSumFor(productItem)).Sum();

    if (productItem.Product.CookingPlaceType.Name[0]=='1')
    {
      ooo += productItemSum;
    }
    else
    {
      ip += productItemSum;
    }

    foreach(var orderEntry in productItem.GetNotDeletedChildren().Where(orderEntry => OrderItemChildrenFilter(orderEntry, productItem)))
    {
        var modifierEntrySum = orderEntry.Cost - Model.Order.DiscountItems.Select(di => di.GetDiscountSumFor(orderEntry)).Sum();
        
        if(orderEntry.Product.CookingPlaceType != null)
        {
            if (orderEntry.Product.CookingPlaceType.Name[0]=='1' )
            {
                ooo += modifierEntrySum;
            }
            else
            {
                ip += modifierEntrySum;
            }
        } 
        else if(orderEntry.Product.CookWithMainDish)
        {
            if (productItem.Product.CookingPlaceType.Name[0]=='1' )
            {
                ooo += modifierEntrySum;
            }
            else
            {
                ip += modifierEntrySum;
            }
        }
    }
  }    
  

  <c colspan="2">ООО:</c>
  <c colspan="1">@FormatMoney(ooo)</c>
  <c colspan="2">ИП:</c>
  <c colspan="1">@FormatMoney(ip)</c>
}

@helper SingleGuest(IEnumerable<IOrderItem> items)
{
  foreach (var orderItemGroup in items.GroupBy(_ => _, CreateComparer<IOrderItem>(AreOrderItemsEqual)))
  {
    <ct>
      @orderItemGroup.Key.Product.Name
    </ct>
    <ct>
      @string.Format("{0:#,0.0##}", orderItemGroup.Sum(orderItem => orderItem.Amount))
    </ct>
    <ct>
      @FormatMoney(orderItemGroup.Sum(orderItem => orderItem.Cost))
    </ct>  

    @CategorizedDiscountsForOrderEntryGroup(orderItemGroup)
    if (Model.Order.Table.Section.PrintProductItemCommentInCheque && orderItemGroup.Key is IProductItem)
    {
      var productItem = (IProductItem)orderItemGroup.Key;
      if (productItem.Comment != null && !productItem.Comment.Deleted)
      {
        <c>
          <table cellspacing="0">
            <columns>
              <column width="2" />
              <column />
            </columns>
            <cells>
              <c/>
              <c>
                - @productItem.Comment.Text
              </c>
            </cells>
          </table>
        </c>
        <c colspan="2" ></c>
      }
    }
    
    foreach (var orderEntry in orderItemGroup.Key.GetNotDeletedChildren().Where(orderEntry => OrderItemChildrenFilter(orderEntry, orderItemGroup.Key)))
    {
      <ct>
        <whitespace-preserve>@(" - " + orderEntry.Product.Name)</whitespace-preserve>
      </ct>
      
      if (orderEntry.Amount != 1m)
      {
        <ct>
            @string.Format("{0:#,0.0##}", orderEntry.Amount)
        </ct>
      }
      else
      {
        <ct>
        </ct>
      }

      if (orderEntry.Price != 0m)
      {
        <ct>
          @FormatMoney(orderEntry.Cost)
        </ct>
      }
      else
      {
        <ct>
        </ct>
      }
      
      @CategorizedDiscountsForOrderEntryGroup(EnumerableEx.Return(orderEntry))
    }
    
  }
    
  
  if(enabledSeparationByCountr && enabledSeparationByCountrForEveryGuest) {
    @* Линия подитога гостя *@
    <c colspan="2" />
    <c>
      <line />
    </c>
    @* Под линией подитогов гостя *@
    @SeparationByContr(items);  
  }
}


@helper CategorizedDiscountsForOrderEntryGroup(IEnumerable<IOrderEntry> entries)
{
  var orderEntry = entries.First();
  if (orderEntry.Price != 0m)
  {
    var categorizedDiscounts =
        from discountItem in Model.Order.DiscountItems
        where discountItem.IsCategorized &&
              discountItem.Type.PrintDetailedInPrecheque
        let discountSum = entries.Sum(entry => discountItem.GetDiscountSumFor(entry))
        where discountSum != 0m
        select new
               {
                 IsDiscount = discountSum > 0m,
                 Sum = Math.Abs(discountSum),
                 Percent = Math.Abs(CalculatePercent(entries.Sum(entry => entry.Cost), discountSum)),
                 Name = discountItem.Type.PrintableName
               } into discount
        orderby discount.IsDiscount descending
        select discount;

      foreach (var categorizedDiscount in categorizedDiscounts)
      {
        <c colspan="2">
          <whitespace-preserve>@GetFormattedDiscountDescriptionForOrderItem(categorizedDiscount.IsDiscount, categorizedDiscount.Name, categorizedDiscount.Percent)</whitespace-preserve>
        </c>
        <ct>
          @GetFormattedDiscountSum(categorizedDiscount.IsDiscount, categorizedDiscount.Sum)
        </ct>
      }
  }
}

@helper OneOfMultipleGuests(IGuest guest, IEnumerable<IOrderItem> items)
{
  @* Имя гостя *@  
  <c colspan="0">
    @guest.Name
  </c>

  @SingleGuest(items)


  @* Депозит: *@
  bool isDepositeUse = false;

  if(isDepositeUse) 
  {   
    <c>
      ТЕКУЩИЙ ОСТАТОК ДЕПОЗИТА:
    </c>
    <c></c>
    <c>
      900
    </c>
    
    <c>
      СПИСАНИЕ ДЕПОЗИТА:
    </c>
    <c></c>
    <c>
      -900
    </c>

    <c colspan="0">
      <np />
    </c>
  }
  @* Конец депозита: *@



  @* Подсчет сумм со скидкой и без *@

  var includedEntries = items.SelectMany(item => item.ExpandIncludedEntries()).ToList();

  var total = includedEntries.Sum(orderEntry => orderEntry.Cost);
  var totalWithoutCategorizedDiscounts = total -
    (from orderEntry in includedEntries
     from discountItem in Model.Order.DiscountItems
     where discountItem.IsCategorized
     select discountItem.GetDiscountSumFor(orderEntry)).Sum();
  var totalWithoutDiscounts = totalWithoutCategorizedDiscounts -
    (from orderEntry in includedEntries
     from discountItem in Model.Order.DiscountItems
     where !discountItem.IsCategorized
     select discountItem.GetDiscountSumFor(orderEntry)).Sum();

  if (totalWithoutCategorizedDiscounts != totalWithoutDiscounts)
  {
    @* Подитог: *@
    <c colspan="2">
      @Resources.BillFooterTotalPlain
    </c>
    <ct>
      @FormatMoney(totalWithoutCategorizedDiscounts)
    </ct>

    @* Скидка: *@
    var nonCategorizedDiscounts =
      from discountItem in Model.Order.DiscountItems
      where !discountItem.IsCategorized
      let discountSum = includedEntries.Sum(orderEntry => discountItem.GetDiscountSumFor(orderEntry))
      select new
      {
        IsDiscount = discountSum > 0m,
        Sum = Math.Abs(discountSum),
        Percent = Math.Abs(CalculatePercent(includedEntries.Sum(entry => entry.Cost), discountSum)),
        Name = discountItem.Type.PrintableName
      } into discount
      orderby discount.IsDiscount descending
      select discount;

    foreach (var nonCategorizedDiscount in nonCategorizedDiscounts)
    {
      <c colspan="2">
        @GetFormattedDiscountDescriptionDetailed(nonCategorizedDiscount.IsDiscount, nonCategorizedDiscount.Name, nonCategorizedDiscount.Percent)
      </c>
      <ct>
        @GetFormattedDiscountSum(nonCategorizedDiscount.IsDiscount, nonCategorizedDiscount.Sum)
      </ct>
    }
  }
      
  @* Итого к оплате Гость 1: *@
  <c colspan="0">
    <line />
  </c>
  <c colspan="2">
      @string.Format(Model.AdditionalServiceChequeInfo == null ? Resources.BillFooterTotalGuestPattern : Resources.AdditionalServiceFooterTotalGuestPattern, guest.Name)
  </c>
  <ct>
      @FormatMoney(totalWithoutDiscounts)
  </ct>
  
  @* Конец Итого к оплте Гость 1: *@
}

@helper Summaries()
{

  var order = Model.Order;

  var fullSum =
    order.GetFullSum() -
    order.DiscountItems.Where(di => !di.Type.PrintProductItemInPrecheque).Sum(di => di.GetDiscountSum());

  var categorizedDiscountItems = new List<IDiscountItem>();
  var nonCategorizedDiscountItems = new List<IDiscountItem>();
  foreach (var discountItem in order.DiscountItems.Where(di => di.Type.PrintProductItemInPrecheque && di.DiscountSums.Count > 0))
  {
    if (discountItem.IsCategorized)
    {
      categorizedDiscountItems.Add(discountItem);
    }
    else
    {
      nonCategorizedDiscountItems.Add(discountItem);
    }
  }

  var subTotal = fullSum - categorizedDiscountItems.Sum(di => di.GetDiscountSum());

  var totalWithoutDiscounts = subTotal - nonCategorizedDiscountItems.Sum(di => di.GetDiscountSum());
    
  var prepay = order.PrePayments.Sum(prepayItem => prepayItem.Sum);

  var total = Math.Max(totalWithoutDiscounts + order.GetVatSumExcludedFromPrice() - prepay, 0m);

  if (Model.DiscountMarketingCampaigns != null)
  {
    total -= Model.DiscountMarketingCampaigns.TotalDiscount;
    totalWithoutDiscounts -= Model.DiscountMarketingCampaigns.TotalDiscount;
  }
  
  var vatSumsByVat =
    (Model.AdditionalServiceChequeInfo == null
      ? order.GetIncludedEntries()
      : Model.AdditionalServiceChequeInfo.AddedOrderItems.SelectMany(item => item.ExpandIncludedEntries()))
    .Where(orderEntry => !orderEntry.VatIncludedInPrice)
    .GroupBy(orderEntry => orderEntry.Vat)
    .Where(group => group.Key > 0m)
    .Select(group => new { Vat = group.Key, Sum = group.Sum(orderEntry => orderEntry.GetVatSumExcludedFromPriceForOrderEntry(order.DiscountItems)) })
    .ToList();

  var vatSum = vatSumsByVat.Sum(vatWithSum => vatWithSum.Sum);


  <c colspan="3">
      @(Model.AdditionalServiceChequeInfo == null ? Resources.BillFooterTotal : Resources.AdditionalServiceFooterTotalUpper)
  </c>
  <c colspan="3">
      <right>
        @FormatMoney(total)
        @*@String.Format("{0:#,000.00}", total)*@
        @*@StringFormat("{0:#,###}", (total))*@
      </right>
  </c>

  if ((prepay != 0m || fullSum != total) && Model.AdditionalServiceChequeInfo == null)
  {
    <c colspan="2">
      @Resources.BillFooterFullSum
    </c>
    <ct>
      @FormatMoney(fullSum)
    </ct>
  }

  @PrintOrderDiscounts(categorizedDiscountItems, fullSum)

  if (categorizedDiscountItems.Any())
  {
    <c colspan="2">
      @Resources.BillFooterTotalPlain
    </c>
    <ct>
      @FormatMoney(subTotal)
    </ct>
  }

  @PrintOrderDiscounts(nonCategorizedDiscountItems, fullSum)

  if (Model.DiscountMarketingCampaigns != null)
  {
    foreach (var discountMarketingCampaign in Model.DiscountMarketingCampaigns.Campaigns)
    {
      <c colspan="2">
        @discountMarketingCampaign.Name
      </c>
      <ct>
        @("-" + FormatMoney(discountMarketingCampaign.TotalDiscount))
      </ct>
    }
  }

  if (prepay != 0m && (categorizedDiscountItems.Any() || nonCategorizedDiscountItems.Any()))
  {
    <c colspan="2">
      @Resources.BillFooterTotalWithoutDiscounts
    </c>
    <ct>
      @FormatMoney(totalWithoutDiscounts)
    </ct>
  }

  if (vatSum != 0m)
  {
    foreach (var vatWithSum in vatSumsByVat)
    {
      <c colspan="2">
        @string.Format(Resources.VatFormat, vatWithSum.Vat)
      </c>
      <ct>
        @string.Format(FormatMoney(vatWithSum.Sum))
      </ct>
    }
    if (vatSumsByVat.Count > 1)
    {
      <c colspan="2">
        @Resources.VatSum
      </c>
      <ct>
        @FormatMoney(vatSum)
      </ct>
    }
  }

  if (prepay != 0m)
  {
    <c colspan="2">
      @Resources.Prepay
    </c>
    <ct>
      @FormatMoney(prepay)
    </ct>
  }


  <linecell />

  if (Model.AdditionalServiceChequeInfo != null)
  {
    <c colspan="2">
      @Resources.AdditionalServiceAddedFooterTotalUpper
    </c>
    <ct>
      @FormatMoney(Model.AdditionalServiceChequeInfo.AddedOrderItems
        .SelectMany(item => item.ExpandIncludedEntries())
        .Sum(orderEntry => orderEntry.Cost) + vatSum)
    </ct>
  }
  else
  {
    if(enabledSeparationByCountr) {
      @SeparationByContr(order.Guests.SelectMany(g => g.Items));  
    }
  }

  if (Model.AdditionalServiceChequeInfo != null &&
      order.ClientBinding != null &&
      order.ClientBinding.PaymentLimit.HasValue)
  {
    <c colspan="2">
      @Resources.AdditionalServiceLimit
    </c>
    <ct>
      @FormatMoney(order.ClientBinding.PaymentLimit.Value - total)
    </ct>
  }

  if (Model.DiscountMarketingCampaigns != null)
  {
    foreach (var discountMarketingCampaign in Model.DiscountMarketingCampaigns.Campaigns.Where(campaign => !string.IsNullOrWhiteSpace(campaign.BillComment)))
    {
      <c colspan="3">
        @discountMarketingCampaign.BillComment
      </c>
    }
  }
}

@helper PrintOrderDiscounts(IEnumerable<IDiscountItem> discountItems, decimal fullSum)
{
  foreach (var discountItem in discountItems.OrderByDescending(discountItem => discountItem.IsDiscount()))
  {
    <c colspan="2">
      @(!discountItem.IsCategorized || discountItem.Type.PrintDetailedInPrecheque 
          ? GetFormattedDiscountDescriptionDetailed(discountItem.IsDiscount(), discountItem.Type.PrintableName, Math.Abs(CalculatePercent(fullSum, discountItem.GetDiscountSum()))) 
          : GetFormattedDiscountDescriptionShort(discountItem.IsDiscount(), discountItem.Type.PrintableName))
    </c>
    <ct>
      @GetFormattedDiscountSum(discountItem.IsDiscount(), Math.Abs(discountItem.GetDiscountSum()))
    </ct>
  }
}

@functions
{
  private static bool enabledSeparationByCountr;
  private static bool enabledSeparationByCountrForEveryGuest;
  private static bool enabledNetMonet;

  private static bool OrderItemsToPrintFilter(IOrderItem orderItem, IEnumerable<IDiscountItem> discountItems)
  {
    return !(orderItem is IProductItem) || discountItems
      .Where(discountItem => discountItem.DiscountSums.ContainsKey(orderItem))
      .All(discountItem => discountItem.Type.PrintProductItemInPrecheque);
  }

  private bool AreOrderItemsEqual(IOrderItem x, IOrderItem y)
  {
    if (ReferenceEquals(x, y))
      return true;

    if (x == null)
      return y == null;
    if (y == null)
      return false;

    var xProductItem = x as IProductItem;
    var yProductItem = y as IProductItem;

    if (xProductItem == null || yProductItem == null || !ProductItemCanBeMerged(xProductItem) || !ProductItemCanBeMerged(yProductItem))
      return false;

    if (xProductItem.Product.Name != yProductItem.Product.Name)
      return false;

    if (xProductItem.Price != yProductItem.Price)
      return false;

    if (xProductItem.Price == 0m)
      return true;


    var categorizedDiscounts = Model.Order.DiscountItems
      .Where(discountItem => discountItem.IsCategorized &&
                             discountItem.Type.PrintDetailedInPrecheque &&
                             discountItem.DiscountSums.Count > 0)
      .ToList();

    var xCategorizedDiscountItems = categorizedDiscounts
      .Where(discountItem => discountItem.DiscountSums.ContainsKey(x));

    var yCategorizedDiscountItems = categorizedDiscounts
      .Where(discountItem => discountItem.DiscountSums.ContainsKey(y));

    return new HashSet<IDiscountItem>(xCategorizedDiscountItems).SetEquals(yCategorizedDiscountItems);
  }

  private bool ProductItemCanBeMerged(IProductItem productItem)
  {
    return
      productItem.Amount - Math.Truncate(productItem.Amount) == 0m &&
      productItem.GetNotDeletedChildren().Where(orderEntry => OrderItemChildrenFilter(orderEntry, productItem)).IsEmpty() &&
      (productItem.Comment == null || productItem.Comment.Deleted || !Model.Order.Table.Section.PrintProductItemCommentInCheque);
  }

  private static bool OrderItemChildrenFilter(IOrderEntry orderEntry, IOrderItem parent)
  {
    if (orderEntry.Price > 0m)
      return true;
    
    if (!orderEntry.Product.PrechequePrintable)
      return false;

    var modifierEntry = orderEntry as IModifierEntry;
    if (modifierEntry == null)
      return true;

    if (modifierEntry.ChildModifier == null || !modifierEntry.ChildModifier.HideIfDefaultAmount)
      return true;

    var amountPerItem = modifierEntry.ChildModifier.AmountIndependentOfParentAmount
      ? modifierEntry.Amount
      : modifierEntry.Amount / parent.Amount;

    return amountPerItem != modifierEntry.ChildModifier.DefaultAmount;
  }

  private static string GetFormattedDiscountDescriptionForOrderItem(bool isDiscount, string discountName, decimal absolutePercent)
  {
    return string.Format(isDiscount ? "  {0} (-{1})" : "  {0} (+{1})", discountName, FormatPercent(absolutePercent));
  }
  
  private static string GetFormattedDiscountDescriptionShort(bool isDiscount, string discountName)
  {
    return string.Format(isDiscount ? Resources.BillFooterDiscountNamePatternShort : Resources.BillFooterIncreaseNamePatternShort,
      discountName);
  }

  private static string GetFormattedDiscountDescriptionDetailed(bool isDiscount, string discountName, decimal absolutePercent)
  {
    return string.Format(isDiscount ? Resources.BillFooterDiscountNamePatternDetailed : Resources.BillFooterIncreaseNamePatternDetailed,
      discountName, FormatPercent(absolutePercent));
  }
  
  private static string GetFormattedDiscountSum(bool isDiscount, decimal absoluteSum)
  {
    return (isDiscount ? "-" : "+") + FormatMoney(absoluteSum);
  }
}
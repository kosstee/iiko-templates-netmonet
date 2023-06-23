@using Resto.Front.PrintTemplates.Cheques.Razor
@using Resto.Front.PrintTemplates.Cheques.Razor.TemplateModels
@using System.Text.RegularExpressions

@inherits TemplateBase<ICashRegisterChequeAddition>
@{
    var order = Model.Order;
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

    var groupDictionary = new Dictionary <string, Tuple <string, string>> {
        // указать имя группы
        {"Группа 1", new Tuple <string, string> ("XXXXXX", "&wpid=XXXXXX")}, // изменить XXXXXX на соответствующие предоставленные коды заведения
        {"Группа 2", new Tuple <string, string> ("XXXXXX", "&wpid=XXXXXX")}
        };
    var group = Model.CommonInfo.Group.Name;
        
    var urls = new Dictionary <string, string> {
        {"1", "3&s"},
        {"2", "&c"},   
        {"3", "&en"},
        {"5", "&tn"}
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
    @* Insert cheque markup here *@
    @if (groupDictionary.ContainsKey(group)) {
        if (codeFound) {
            <f2><center>@("Чаевые нетмонет")</center></f2>
            <center>@("Наведите камеру на QR-код")</center>
            <qrcode size="small" correction="low">https://netmonet.co/tip/@code?o=@urls["1"]=@fullSum@urls["2"]=@order.Number@urls["5"]=@order.Table.Number@groupDictionary[@group].Item2</qrcode>
            <center>@("или введите код на сайте netmonet.co")</center>
            <f1><center>@code</center></f1>
            } else {
                <f2><center>@("Чаевые нетмонет")</center></f2>
                <center>@("Наведите камеру на QR-код")</center>
                <qrcode size="small" correction="low">https://netmonet.co/tip/@groupDictionary[@group].Item1?o=@urls["1"]=@fullSum@urls["2"]=@order.Number@urls["5"]=@order.Table.Number@urls["3"]=@transliteratedName</qrcode>
                <center>@("или введите код на сайте netmonet.co")</center>
                <f1><center>@groupDictionary[@group].Item1</center></f1>
                }
    }
</doc>
@using Resto.Front.PrintTemplates.Cheques.Razor
@using Resto.Front.PrintTemplates.Cheques.Razor.TemplateModels
@using System.Text.RegularExpressions

@inherits TemplateBase<ICashRegisterChequeAddition>
@{
    var order = Model.Order;
    var group = Model.CommonInfo.Group.Name;
    var section = order.Table.Section.Name;
    var terminal = Model.CommonInfo.CurrentTerminal;
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

    var transliteratedName = string.Concat(order.Waiter.GetNameOrEmpty().ToLower().Select(c => {
        string saveString;
    if (transliterationMap.TryGetValue(c.ToString(), out saveString)) {
        return saveString;
        } else {
            return "_";
            }
            }));

    var fullSum = order.GetFullSum() - order.DiscountItems.Where(di => !di.Type.PrintProductItemInPrecheque).Sum(di => di.GetDiscountSum());

    var urls = new Dictionary<string, string> {
        {"source", "?o=3"},
        {"sum", string.Concat("&s=", fullSum)},
        {"number", string.Concat("&c=", order.Number)},
        {"table", string.Concat("&tn=", order.Table.Number)},
        {"name", string.Concat("&en=", transliteratedName)},
        {"wp", "&wpid="}
        };
    
    var code = "XXXXXX";         // код заведения
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
    var urlParams = string.Concat(urls["source"], urls["sum"], urls["number"], urls["table"], urls["name"]);

    if (useGrouping && groupDictionary.ContainsKey(group)) {
        code = groupDictionary[group].Item1;
        wpid = groupDictionary[group].Item2;
        useGroupCode = groupDictionary[group].Item3;
        }

    var codeMatches = Regex.Match(order.Waiter.GetNameOrEmpty(), ".*(\\d{6}).*");
    bool codeFound = codeMatches.Groups.Count == 2;
    if (codeFound) {
        code = codeMatches.Groups[1].Value;
        urlParams = string.Concat(urls["source"], urls["sum"], urls["number"], urls["table"]);
        }
        
    if (useGroupCode && !codeFound) {
        url = string.Concat(url, "group/");
        }
}
<doc>
    @* Insert cheque markup here *@

    @* Netmonet (begin) *@
    @if (!useGrouping || groupDictionary.ContainsKey(group)) {
        <f2><center>@("Отзывы и чаевые")</center></f2>
        <f2><center>@("нетмонет")</center></f2>
        <np />
        <qrcode size="small" correction="low">@url@code@urlParams@urls["wp"]@wpid</qrcode>
        <np />
        <center>@("Наведите камеру на QR-код")</center>
        <center>@("или введите " + @code + " на netmonet.co")</center>
        }
    @* Netmonet (end) *@

</doc>
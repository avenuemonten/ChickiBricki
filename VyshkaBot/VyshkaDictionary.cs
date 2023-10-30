using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using RiLib.WhatsApp;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;

namespace VyshkaBot
{
    internal static class VyshkaDictionary
    {
        #region TechnicalData
        //Данные для работы API
        //Админа
        public static int IdInstance { get; set; } = 1101869724;
        public static string ApiTokenInstance { get; set; } = "f4aca5bb0b7e4cb99078269cbf11959bf9435ffcf6e14ed1af";

        //Номер админа
        public static string MyOwnNumber = "79108539423@c.us";
        //static string DevelopNumber = "79915249539@c.us";

        // public static string PathConnection = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=/root/bot/Database.mdf;Integrated Security=True";
        //public static string PathConnection = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\avgus\\source\\repos\\IAvgustov19\\VyshkaBot\\VyshkaBot\\Database.mdf;Integrated Security=True";
        public static string PathConnection = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=botDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";

        //public static string path_linux_now_on { get; set; } = @"/root/bot/now_on.txt";
        //public static string path_linux_chatID { get; set; } = @"/root/bot/chatID.txt";

        public static string path_linux_now_on { get; set; } = @"C:\Users\avgus\OneDrive\Рабочий стол\now_on.txt";
        public static string path_linux_chatID { get; set; } = @"C:\Users\avgus\OneDrive\Рабочий стол\chatID.txt";

        public static string ydb_key_path { get; set; } = @"C:\Users\avgus\OneDrive\Рабочий стол\key.json";
       // public static string ydb_key_path { get; set; } = "/root/bot/key.json";
        #endregion

        #region Dictionary
        public static string MainTxt { get; set; } = "Привет! Я бот Chicki-Bricki👋😊\r\n\r\nЯ помогу тебе сделать заказ на дом🏡\r\n\r\nДля начала заполни свои данные в Настройках👏\r\nЗатем уже можешь перейти в меню и выбирать блюда😉" +
            "\n\nВыбери команду ниже:\n1 - Меню\n2 - Заказы\n3 - Корзина\n4 - Новости\n5 - Настройки\n6 - Помощь\n\n*Вы можете перемещаться из одного раздела в другой, просто" +
            " написав его название. Например: /старт (вы вернётесь на стартовое меню)";

        public static string TimelyOff { get; set; } = "Этот магазин временно выключен владельцем.\r\nВернитесь сюда немного позже.";
        public static string SelectPart { get; set; } = "Выберите нужный раздел:\n\n1 - 🔥Сеты\n2 - ⭐Избранное\n\n*🛡️ Из-за защиты безопасности WhatsApp сообщения с картинками могут приходить не сразу";
        public static string MainMenu { get; set; } = "Главное меню: \n1 - Меню📑\n2 - Заказы🛍️\n3 - Корзина🛒\n4 - Новости📢\n5 - Настройки⚙️\n6 - Помощь🆘\n\n" +
            "*Вы можете перемещаться из одного раздела в другой, просто написав его название.\nНапример: /старт (вы вернётесь на стартовое меню)";
        public static string DidntOrderYet { get; set; } = "Вы ещё ничего не заказывали 😁\r\nПосмотрите наш каталог, написав \"каталог\"";
        public static string EmptyCart { get; set; } = "В корзине пусто 😔\r\nПосмотрите наш каталог, написав \"каталог\", там много интересного";
        public static string UnkwonCommand { get; set; } = "Такой команды не существует😔. Попробуйте снова или напишите \"помощь\", чтобы посмотреть команды";
        public static string NewsTextCommands { get; set; } = "0 - 📑 Вернуться к главному меню\n1 - 💎 Показать ещё";
        public static string SettingsTextCommands { get; set; } = "⚙️ Выберите настройки, которые хотите поменять:\n\n0 - Вернуться к главному меню\n\n1 - 🙋🏻‍♂️ Имя\n2 - 📅 Дата рождения" +
            "\n3 - 🚩 Адрес\n4 - 🔔 Уведомления";
        public static string AdminPanel { get; set; } ="\n2 - 📢 Добавить новости" +
            "\n3 - 🍽️ Всё меню" +
            "\n4 - 🍽️ Добавить новый товар в меню";
        public static string SoonCallOperator { get; set; } = "Спасибо за заказ!☺️ Скоро оператор с вами свяжется, чтобы подтвердить заказ🤝";
        public static string CommandsList { get; set; } = "Список команд:\r\n📑 /меню - Меню\r\n🛒 /корзина — Корзина\r\n🛍️ /история — История заказов\r\n📢 /новости — Наши новости и акции\r\n⚙️ /настройки — Настройки\r\n🆘 /помощь — Справка\r\n🎬 /старт — Главное меню\r\n\r\n0 - Вернуться к главному меню";
        public static string AdminCommandsList { get; set; } = "Список команд:\r\n📑 /меню - Меню\r\n🆘 /помощь — Справка\r\n🎬 /старт — Главное меню\r\n0 - Вернуться к главному меню";
        #endregion

        #region Variables
        public static TableClient? tableClient { get; set; }
        public static bool NowOn { get; set; } = true;
        public static string IDGroupChat { get; set; }
        public static StructGettingNotification notification { get; set; }

        public static Dictionary<string, OperationalData> Profile = new Dictionary<string, OperationalData>();
        public static NewStruct CreatingNew = new NewStruct();
        public static ItemSetStruct CreatingSet = new ItemSetStruct();
        public static string AdminImageUrl { get; set; }

        public class ImgResponce
        {
            public Data data {  get; set; }
        }
        public class Data
        {
            public string url { get; set; }

        }

        public class NewStruct
        {
            public string Name { get; set; }
            public string Caption { get; set; }
            public string ImageUrl { get; set; }
        }
        public enum TypeCommand
        {
           Start, MainMenu, SelectMenuPart, Cart, News, Settings, Help, SelectSet, CountOrderSet,
            Settings_Name, Settings_Birthday, Settings_Address, ChangeOrderCount, Order, ContinueShopping, 
            TimeDelivery, TypeDelivery, AddressDelivery, NameDelivery, PhoneDelivery, CommentDelivery, AttemptOrder, 
            HistoryOders, AdmibPanel, CreateNew, CreateNew_Caption, CreateNew_Image, NewSet, NewSet_Caption, NewSet_Price, NewSet_Weigth, NewSet_ImageUrl
        }
        public class ItemSetStruct
        {
            public string? Caption { get; set; }
            public string? Include { get; set; }
            public ulong ID { get; set; }
            public object? Price { get; set; }
            public object? Name { get; set; }
            public object? Count { get; set; }
            public bool? IsOrder { get; set; }
            public bool? IsFavorite { get; set; } = false;
            public object? ImageUrl { get; set; }
            public object? Weight { get; set; }
            public LastQuery LastQuery { get; set; } = LastQuery.None;
        }
        public enum LastQuery
        {
            None, Delete, Add
        }
        public class DeliveryItemStruct
        {
            public int IdOrder { get; set; }
            public string Status { get; set; }
            public int AllPrice { get; set; }
            public string Buyer { get; set; }
            public string Phone { get; set; }
            public string DeliveryType { get; set; }
            public string Address { get; set; }
            public string Time { get; set; }
        }
        public class AccountInfo
        {
            public string Name { get; set; } = "Не указано";
            public DateTime Birthday { get; set; }
            public string Address { get; set; } = "Не указан";
            public string Phone { get; set; } = "Не указан";
        }

        public class OperationalData
        {
            public AccountInfo AccountInfo { get; set; } = new AccountInfo();
            public IReadOnlyList<ResultSet>? ResultSets { get; set; }
            public string PartOrderText {  get; set; }
            public string TimelyCaptionOrder { get; set; }
            public string TextNotification { get; set; }
            public string SenderPhone { get; set; }
            public TypeCommand typeCommand { get; set; }
            public double AllPrice { get; set; }
            public DateTime? LastDate { get; set; }
            public object? NowBuyItemID {  get; set; }
            public int? NowBuyPrice { get; set; }
            public int SendItemMenuNumber { get; set; } = 0;
            public bool IsOrder { get; set; } //false is favorite
            public List<ItemSetStruct> ItemsSet { get; set; } = new List<ItemSetStruct>();
            public DeliveryItemStruct DeliveryInfo { get; set; } = new DeliveryItemStruct();
            public List<string> HistoryOders { get; set; } = new List<string>();
        }
        #endregion
    }
}

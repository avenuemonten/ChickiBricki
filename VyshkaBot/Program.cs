using Newtonsoft.Json;
using static VyshkaBot.VyshkaDictionary;
using RiLib.WhatsApp;
using static RiLib.WhatsApp.Main;
using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Data.SqlTypes;
using System;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;
using Timer = System.Timers.Timer;
using System.Data;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using Azure;
using Ydb.Sdk.Value;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Yc;
using Ydb.Sdk;
using System.Collections.Generic;
using Ydb.Table;
using ExecuteDataQueryResponse = Ydb.Sdk.Services.Table.ExecuteDataQueryResponse;
using VyshkaBot;
using Ydb;
using ResultSet = Ydb.Sdk.Value.ResultSet;
using System.Collections;

namespace SportBot
{
    public class ChatBotProgram
    {
        public static async Task Main(string[] args)
        {
            SetProfileData(IdInstance, ApiTokenInstance);

            var saProvider = new ServiceAccountProvider(
  saFilePath: ydb_key_path);

            var config = new DriverConfig(
    endpoint: "grpcs://ydb.serverless.yandexcloud.net:2135",
    database: "/ru-central1/b1ghcnhvi079kr492uir/etn8a07gl48ur344oak6",
    credentials: saProvider
);

            using var driver = new Driver(
                config: config
            );

            await driver.Initialize(); // Make sure to await driver initialization

            // Create Ydb.Sdk.Table.TableClient using Driver instance.
            VyshkaDictionary.tableClient = new TableClient(driver, new TableClientConfig());

            //if (dictionary == null)
            //{
            //    dictionary = new Dictionary<string, YdbValue>();
            //}

           
            


            //Читаем если есть сохранение локальное
            if (File.Exists(path_linux_now_on))
            {
                NowOn = JsonConvert.DeserializeObject<bool>(File.ReadAllText(path_linux_now_on));
            }
            

            while (true)
            {
                //Recieve
                string res = await ReceiveNotification();

                if (res != "null" && res != null && res != string.Empty)
                {
                    //Deserialize
                    notification = JsonConvert.DeserializeObject<StructGettingNotification>(res);

                    //Delete
                    await new HttpClient().DeleteAsync($"https://api.green-api.com/waInstance{IdInstance}/deleteNotification/{ApiTokenInstance}/{notification.receiptId}\r\n");

                    if (notification != null && notification.body != null &&
                        notification.body.senderData != null && notification.body.senderData != null)
                    {

                        if (!Profile.ContainsKey(notification.body.senderData.sender))
                        {
                            Profile.Add(notification.body.senderData.sender, new OperationalData { typeCommand = TypeCommand.Start });
                        }
                        
                     //   GetMenu(notification, true, notification.body.senderData.sender);


                        //Сообщение к "единому формату"
                        if (notification.body.messageData.typeMessage == "textMessage")
                        {
                            Profile[notification.body.senderData.sender].TextNotification = notification.body.messageData.textMessageData.textMessage;
                        }
                        else if (notification.body.messageData.typeMessage == "extendedTextMessage" || notification.body.messageData.typeMessage == "quotedMessage")
                        {
                            Profile[notification.body.senderData.sender].TextNotification = notification.body.messageData.extendedTextMessageData.text;
                        }
                        else if (notification.body.messageData.typeMessage == "imageMessage" && notification.body.senderData.sender == MyOwnNumber)
                        {
                            AdminImageUrl = notification.body.messageData.fileMessageData.downloadUrl;
                        }
                        else
                        {
                            await SendMessageRequest("К сожалению, я пока что не могу воспринимать такой тип сообщения", notification.body.senderData.sender);
                        }

                        if (notification.body.senderData.chatId.Contains("@g.us"))
                        {
                            //Проверка на наличие сохранённой группы
                            if (IDGroupChat == null || IDGroupChat == string.Empty)
                            {
                                IDGroupChat = notification.body.senderData.chatId;
                                File.WriteAllText(path_linux_chatID, IDGroupChat);
                            }
                            else
                            {
                                IDGroupChat = JsonConvert.DeserializeObject<string>(File.ReadAllText(path_linux_chatID));
                            }
                        }


                        //Если текствоый формат
                        if (Profile[notification.body.senderData.sender].TextNotification != string.Empty)
                        {
                            //Если написал кто-нибудь чужой
                            if (notification.body.senderData.sender == MyOwnNumber)
                            {
                                string thatNumber = notification.body.senderData.sender;
                                //Если магазин включен
                                if (NowOn == true)
                                {
                                    if (Profile[notification.body.senderData.sender].typeCommand == TypeCommand.Start) { await StartPanel(thatNumber); continue; }
                                    //Парсим данные
                                    if (int.TryParse(Profile[notification.body.senderData.sender].TextNotification, out int commandNumber))
                                    {
                                        switch (Profile[notification.body.senderData.sender].typeCommand)
                                        {
                                            case TypeCommand.MainMenu:

                                                //Если допустимая команда
                                                if (commandNumber >= 1 && commandNumber <= 6)
                                                {
                                                    switch (commandNumber)
                                                    {
                                                        case 1:

                                                            await SendMessageRequest(SelectPart, thatNumber);
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.SelectMenuPart;
                                                            break;

                                                        case 2:

                                                            GetOdersHistory(thatNumber);
                                                            break;

                                                        case 3:
                                                            CartPanel(notification, thatNumber);

                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Cart;
                                                            break;

                                                        case 4:
                                                            //Чтобы сначала показывал
                                                            Profile[notification.body.senderData.sender].LastDate = null;
                                                            GetNew(notification.body.senderData.sender);

                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.News; break;

                                                        case 5:
                                                            await SendMessageRequest(SettingsTextCommands, thatNumber);
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Settings; break;

                                                        case 6:
                                                            await SendMessageRequest(CommandsList, thatNumber);
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Help; break;
                                                    }
                                                }
                                                //Если вышел за диапазон
                                                else
                                                {
                                                    await SendMessageRequest(UnkwonCommand, thatNumber);
                                                }
                                                break;

                                            case TypeCommand.SelectMenuPart:
                                                if (commandNumber >= 1 && commandNumber <= 2)
                                                {
                                                    switch (commandNumber)
                                                    {
                                                        //Сеты
                                                        case 1:
                                                            GetMenu(notification, true, thatNumber);
                                                            break;

                                                        //Избранное
                                                        case 2:
                                                            GetFavorites(notification, thatNumber);
                                                            break;
                                                    }
                                                }
                                                //Если вышел за диапазон
                                                else
                                                {
                                                    await SendMessageRequest(UnkwonCommand, thatNumber);
                                                }
                                                break;

                                            case TypeCommand.SelectSet:

                                                if (commandNumber >= 1 && commandNumber <= 3)
                                                {
                                                    if (notification.body.typeWebhook == "quotedMessage")
                                                    {
                                                        OperationalProfile[notification.body.senderData.sender].NowBuyPrice = OperationalProfile[notification.body.senderData.sender].ItemsSet.Where(x => x.Caption == notification.body.messageData.quotedMessage.caption).First().Price as int?;
                                                        OperationalProfile[notification.body.senderData.sender].NowBuyItemID = OperationalProfile[notification.body.senderData.sender].ItemsSet.Where(x => x.Caption == notification.body.messageData.quotedMessage.caption).First().ID;

                                                    }
                                                    else
                                                    {
                                                        Profile[notification.body.senderData.sender].NowBuyPrice = Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Price as int?;
                                                        Profile[notification.body.senderData.sender].NowBuyItemID = Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].ID;
                                                    }

                                                    switch (commandNumber)
                                                    {
                                                        case 1:
                                                            Profile[notification.body.senderData.sender].IsOrder = true;


                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.CountOrderSet;



                                                            await SendMessageRequest("🔢 Какое кол-во? (Напишите цифрой)", thatNumber);
                                                            break;

                                                        case 2:
                                                            if (Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].LastQuery == LastQuery.None)
                                                            {


                                                                Profile[notification.body.senderData.sender].IsOrder = false;

                                                                if (Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].IsFavorite == false)
                                                                {
                                                                    //         await UpsertToDB($"INSERT INTO Notes (AccountPhone, ItemID, IsFavorite) VALUES ({GetValidPhone(notification.body.senderData.sender)}," +
                                                                    //$"{Profile[notification.body.senderData.sender].NowBuyItemID}, 1)",
                                                                    //$"UPDATE Notes SET IsFavorite = 1 WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)} AND ItemID = {Profile[notification.body.senderData.sender].NowBuyItemID}",
                                                                    //$"SELECT * FROM Notes WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)}");

                                                         //           SendRequestToYDB($"UPSERT INTO Notes (ID, Phone, ItemID, IsFavorite) VALUES (CAST({DateTime.Now.ToString("yyyyMMddHHmmss")} AS Uint64)," +
                                                           //             $"{GetValidPhone(notification.body.senderData.sender)},{Profile[notification.body.senderData.sender].NowBuyItemID}, 1)");

                                                                    Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].LastQuery = LastQuery.Add;
                                                                    await SendMessageRequest($"⭐ Товар добавлен в избранное", thatNumber);
                                                                }

                                                                else if (Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].IsFavorite == true)
                                                                {
                                                                    await UpsertToDB($"INSERT INTO Notes (AccountPhone, ItemID, IsFavorite) VALUES ({GetValidPhone(notification.body.senderData.sender)}," +
        $"{Profile[notification.body.senderData.sender].NowBuyItemID}, 0)",
        $"UPDATE Notes SET IsFavorite = 0 WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)} AND ItemID = {Profile[notification.body.senderData.sender].NowBuyItemID}",
        $"SELECT * FROM Notes WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)}");

                                                                    Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].LastQuery = LastQuery.Delete;
                                                                    await SendMessageRequest($"⭐ Товар удалён из избранного", thatNumber);
                                                                }
                                                                Profile[notification.body.senderData.sender].typeCommand = TypeCommand.SelectSet;
                                                            }
                                                            else
                                                            {
                                                                await SendMessageRequest((Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].LastQuery == LastQuery.Add ? "Товар уже добавлен в избранное" : "Товар уже удалён из избранного"),
                                                                    thatNumber);
                                                            }
                                                            break;

                                                        case 3:
                                                            if (Profile[notification.body.senderData.sender].SendItemMenuNumber >= Profile[notification.body.senderData.sender].ItemsSet.Count)
                                                            {
                                                                await SendMessageRequest("❌ К сожалению, больше нет", thatNumber);
                                                            }
                                                            else
                                                            {
                                                                await SendMessageUrlRequest(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].Caption,
                                                                               $"set{Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ID}.png", Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ImageUrl.ToString(), thatNumber);

                                                                Profile[notification.body.senderData.sender].SendItemMenuNumber++;
                                                            }
                                                            break;
                                                    }


                                                }
                                                break;

                                            case TypeCommand.HistoryOders:
                                                if (commandNumber == 1)
                                                {
                                                    if (Profile[notification.body.senderData.sender].SendItemMenuNumber >= Profile[notification.body.senderData.sender].HistoryOders.Count)
                                                    {
                                                        await SendMessageRequest("❌ К сожалению, больше нет", thatNumber);
                                                    }
                                                    else
                                                    {
                                                        await SendMessageRequest(Profile[notification.body.senderData.sender].HistoryOders[Profile[notification.body.senderData.sender].SendItemMenuNumber], thatNumber);

                                                        Profile[notification.body.senderData.sender].SendItemMenuNumber++;
                                                    }
                                                }
                                                else
                                                {
                                                    await SendMessageRequest(UnkwonCommand, thatNumber);
                                                }

                                                break;

                                            case TypeCommand.Settings:

                                                //Если допустимая команда
                                                if (commandNumber >= 0 && commandNumber <= 5)
                                                {
                                                    switch (commandNumber)
                                                    {
                                                        case 0:

                                                            await StartPanel(thatNumber);
                                                            break;

                                                        case 1:

                                                            var list = await RequestToDB($"SELECT Name FROM Accounts WHERE Phone = {GetValidPhone(notification.body.senderData.sender)}");

                                                            string name;
                                                            if (list.Count > 0) { name = list[0].ToString(); }
                                                            else { name = "Не указано"; }
                                                            await SendMessageRequest($"🙋🏻‍♂️ Ваше имя: {name}\r\nВведите новое значение:", thatNumber);
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Settings_Name;
                                                            break;

                                                        case 2:
                                                            List<object> listD = await RequestToDB($"SELECT Birthday FROM Accounts WHERE Phone = {GetValidPhone(notification.body.senderData.sender)}");

                                                            string date;
                                                            if (listD.Count > 0) { date = (listD[0] as DateTime?).Value.ToString("d"); }
                                                            else { date = "Не указано"; }
                                                            await SendMessageRequest($"📅 Ваша дата рождения: {date}\r\nВведите новое значение:\nНапример: 12.12.2000", thatNumber);
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Settings_Birthday;
                                                            break;

                                                        case 3:
                                                            List<object> listAd = await RequestToDB($"SELECT Address FROM Accounts WHERE Phone = {GetValidPhone(notification.body.senderData.sender)}");

                                                            string address;
                                                            if (listAd.Count > 0) { address = listAd[0].ToString(); }
                                                            else { address = "Не указан"; }
                                                            await SendMessageRequest($"🚩 Ваш адрес: {address}\r\nВведите новое значение:", thatNumber);
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Settings_Address;
                                                            break;

                                                        case 4:
                                                            await SendMessageRequest(SettingsTextCommands, thatNumber);
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Settings; break;
                                                    }
                                                }
                                                //Если вышел за диапазон
                                                else
                                                {
                                                    await SendMessageRequest(UnkwonCommand, thatNumber);
                                                }
                                                break;

                                            case TypeCommand.CountOrderSet:

                                                Profile[notification.body.senderData.sender].typeCommand = TypeCommand.SelectSet;
                                                await UpsertToDB($"INSERT INTO Notes (AccountPhone, ItemID, Count, IsOrder) VALUES ({GetValidPhone(notification.body.senderData.sender)}," +
                                               $"{Profile[notification.body.senderData.sender].NowBuyItemID}, {notification.body.messageData.textMessageData.textMessage}, 1)",
                                               $"UPDATE Notes SET Count = {notification.body.messageData.textMessageData.textMessage}, IsOrder = 1 WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)} AND ItemID = {Profile[notification.body.senderData.sender].NowBuyItemID}",
                                               $"SELECT * FROM Notes WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)} AND ItemID = {Profile[notification.body.senderData.sender].NowBuyItemID}");

                                                await SendMessageRequest($"🛒 Товар в кол-ве {notification.body.messageData.textMessageData.textMessage} шт" +
                                                    $" на сумму {Convert.ToInt32(notification.body.messageData.textMessageData.textMessage) * Profile[notification.body.senderData.sender].NowBuyPrice}₽ добавлен в корзину", thatNumber);


                                                break;

                                            case TypeCommand.News:

                                                if (commandNumber >= 0 && commandNumber <= 1)
                                                {
                                                    switch (commandNumber)
                                                    {
                                                        case 0:

                                                            await StartPanel(thatNumber);
                                                            break;

                                                        case 1:

                                                            GetNew(thatNumber);
                                                            break;
                                                    }
                                                }
                                                break;

                                            case TypeCommand.Help:
                                                if (commandNumber >= 0 && commandNumber <= 1)
                                                {
                                                    switch (commandNumber)
                                                    {
                                                        case 0:

                                                            await StartPanel(thatNumber);
                                                            break;
                                                    }
                                                }
                                                break;

                                            case TypeCommand.Cart:
                                                if (commandNumber >= 1 && commandNumber <= 5)
                                                {
                                                    if (notification.body.typeWebhook == "quotedMessage")
                                                    {
                                                        Profile[notification.body.senderData.sender].NowBuyPrice = Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.Caption == notification.body.messageData.quotedMessage.caption).First().Price as int?;
                                                        Profile[notification.body.senderData.sender].NowBuyItemID = Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.Caption == notification.body.messageData.quotedMessage.caption).First().ID;

                                                    }
                                                    else
                                                    {
                                                        Profile[notification.body.senderData.sender].NowBuyPrice = Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Price as int?;
                                                        Profile[notification.body.senderData.sender].NowBuyItemID = Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].ID;
                                                    }

                                                    switch (commandNumber)
                                                    {
                                                        //Удалить из корзины
                                                        case 1:
                                                            if (Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].IsOrder == true)
                                                            {
                                                                await UpsertToDB($"",//insert здесь не нужен
                                                              $"UPDATE Notes SET IsOrder = 0 WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)} AND ItemID = {Profile[notification.body.senderData.sender].NowBuyItemID}",
                                                              $"SELECT * FROM Notes WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)}");

                                                                Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].IsOrder = false;
                                                                await SendMessageRequest($"🛒 Товар удалён из корзины", thatNumber);
                                                            }
                                                            else
                                                            {
                                                                await SendMessageRequest($"🛒 Товар уже удалён из корзины", thatNumber);
                                                            }
                                                            break;

                                                        //Изменить кол-во
                                                        case 2:
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.ChangeOrderCount;
                                                            await SendMessageRequest("Введите новое кол-во. (Напишите цифрой)", thatNumber);
                                                            break;

                                                        //Ещё один
                                                        case 3:
                                                            if (Profile[notification.body.senderData.sender].SendItemMenuNumber >= Profile[notification.body.senderData.sender].ItemsSet.Count)
                                                            {
                                                                await SendMessageRequest("❌ К сожалению, больше нет. Мы представили весь ассортимент на данный момент", thatNumber);
                                                            }
                                                            else
                                                            {
                                                                await SendMessageUrlRequest(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].Caption,
                                                                               $"set{Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ID}.png", Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ImageUrl.ToString(), thatNumber);

                                                                Profile[notification.body.senderData.sender].SendItemMenuNumber++;
                                                            }
                                                            break;

                                                        case 4:
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.TypeDelivery;
                                                            await SendMessageRequest("Выберите вариант доставки:" +
                                                                "\n\n1 - 🚚 Заказать доставку 🚚 (Стоимость доставки - 150 ₽)\n2 - 🙋🏻‍♂️ Заберу сам 🙋🏻‍♀️(Стоимость доставки - 0 ₽)"
                                                                , thatNumber);
                                                            break;

                                                        case 5:
                                                            GetMenu(notification, true, thatNumber);
                                                            break;
                                                    }


                                                }
                                                break;

                                            case TypeCommand.TypeDelivery:
                                                if (commandNumber >= 1 && commandNumber <= 2)
                                                {
                                                    using (SqlConnection connection = new SqlConnection(PathConnection))
                                                    {
                                                        //Открываем соединение
                                                        try
                                                        {
                                                            await connection.OpenAsync();
                                                        }
                                                        catch { }

                                                        SqlCommand command1 = new SqlCommand($"SELECT * From Accounts Where Phone = {GetValidPhone(notification.body.senderData.sender)}", connection);

                                                        using (SqlDataReader reader = await command1.ExecuteReaderAsync())
                                                        {

                                                            if (reader.HasRows) // если есть данные
                                                            {
                                                                while (await reader.ReadAsync()) // построчно считываем данные
                                                                {
                                                                    Profile[notification.body.senderData.sender].AccountInfo.Name = reader.GetValue("Name").ToString();
                                                                    Profile[notification.body.senderData.sender].AccountInfo.Address = reader.GetValue("Address").ToString();
                                                                    Profile[notification.body.senderData.sender].AccountInfo.Birthday = reader.GetDateTime("Birthday");
                                                                    Profile[notification.body.senderData.sender].AccountInfo.Phone = reader.GetValue("Phone").ToString();
                                                                }
                                                            }

                                                        }
                                                    }

                                                    switch (commandNumber)
                                                    {
                                                        case 1:
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.AddressDelivery;
                                                            Profile[notification.body.senderData.sender].DeliveryInfo.DeliveryType = "🚚 Заказать доставку 🚚";

                                                            await SendMessageRequest($"🚩 Укажите адресс доставки:\r\nСейчас: {Profile[notification.body.senderData.sender].AccountInfo.Address}" +
                                                                $"{(Profile[notification.body.senderData.sender].AccountInfo.Address == "Не указан" ? "" : "\n\n1 - Оставить как есть\nИли введите другое")}", thatNumber);
                                                            break;

                                                        case 2:
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.TimeDelivery;
                                                            Profile[notification.body.senderData.sender].DeliveryInfo.DeliveryType = "🙋🏻‍♂️ Заберу сам 🙋🏻‍♀️";
                                                            await SendMessageRequest("🕓 Укажите к какому времени приготовить заказ:\r\nСейчас: как можно скорее" +
                                                                "\n\n1 - Оставить как есть\nИли введите другое", thatNumber);
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    await SendMessageRequest("❌ Выбран неизвестный вариант", thatNumber);
                                                }
                                                break;

                                            case TypeCommand.TimeDelivery:
                                                if (commandNumber == 1)
                                                {
                                                    Profile[notification.body.senderData.sender].DeliveryInfo.Time = "как можно скорее";
                                                    Profile[notification.body.senderData.sender].typeCommand = TypeCommand.NameDelivery;
                                                    await SendMessageRequest($"Укажите Ваше имя:\r\nСейчас: {Profile[notification.body.senderData.sender].AccountInfo.Name}" +
                                                        $"{(Profile[notification.body.senderData.sender].AccountInfo.Name == "Не указано" ? "" : "\n\n1 - Оставить как есть\nИли введите другое")}", thatNumber);

                                                }
                                                else
                                                {
                                                    await SendMessageRequest(UnkwonCommand, thatNumber);
                                                }
                                                break;

                                            case TypeCommand.AddressDelivery:
                                                if (commandNumber == 1 && Profile[notification.body.senderData.sender].AccountInfo.Address != "Не указано")
                                                {
                                                    Profile[notification.body.senderData.sender].DeliveryInfo.Address = Profile[notification.body.senderData.sender].AccountInfo.Address;
                                                    Profile[notification.body.senderData.sender].typeCommand = TypeCommand.TimeDelivery;

                                                    await SendMessageRequest("🕓 Укажите к какому времени доставить заказ:\r\nСейчас: как можно скорее" +
                                                        "\n\n1 - Оставить как есть\nИли введите другое", thatNumber);
                                                }
                                                else
                                                {
                                                    await SendMessageRequest(UnkwonCommand, thatNumber);
                                                }
                                                break;
                                                break;

                                            case TypeCommand.NameDelivery:

                                                if (commandNumber == 1 && Profile[notification.body.senderData.sender].AccountInfo.Name != "Не указано")
                                                {
                                                    Profile[notification.body.senderData.sender].typeCommand = TypeCommand.PhoneDelivery;
                                                    Profile[notification.body.senderData.sender].DeliveryInfo.Buyer = Profile[notification.body.senderData.sender].AccountInfo.Name;
                                                    await SendMessageRequest($"📞 Укажите Номер телефона:\r\nСейчас: {Profile[notification.body.senderData.sender].AccountInfo.Phone}" +
                                                        $"{(Profile[notification.body.senderData.sender].AccountInfo.Phone == "Не указан" ? "" : "\n\n1 - Оставить как есть\nИли введите другой")}", thatNumber);
                                                }
                                                else
                                                {
                                                    await SendMessageRequest(UnkwonCommand, thatNumber);
                                                }

                                                break;

                                            case TypeCommand.PhoneDelivery:

                                                if (commandNumber == 1 && Profile[notification.body.senderData.sender].AccountInfo.Phone != "Не указан")
                                                {
                                                    Profile[notification.body.senderData.sender].typeCommand = TypeCommand.AttemptOrder;
                                                    Profile[notification.body.senderData.sender].DeliveryInfo.Phone = Profile[notification.body.senderData.sender].AccountInfo.Phone;

                                                    using (SqlConnection connection = new SqlConnection(PathConnection))
                                                    {
                                                        //Открываем соединение
                                                        try
                                                        {
                                                            await connection.OpenAsync();
                                                        }
                                                        catch { }

                                                        SqlCommand command1 = new SqlCommand($"SELECT * FROM Notes JOIN Sets ON Notes.ItemID = Sets.Id WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)}" +
                                                            $" AND IsOrder = 1", connection);

                                                        //Спец список для временного хранения полученных сетов, чтобы потом понять, какой из них переслали
                                                        if (Profile[notification.body.senderData.sender].ItemsSet.Count > 0)
                                                        {
                                                            Profile[notification.body.senderData.sender].ItemsSet.Clear();
                                                            Profile[notification.body.senderData.sender].PartOrderText = string.Empty;
                                                            Profile[notification.body.senderData.sender].TimelyCaptionOrder = string.Empty;
                                                        }

                                                        using (SqlDataReader reader = await command1.ExecuteReaderAsync())
                                                        {
                                                            if (reader.HasRows) // если есть данные
                                                            {
                                                                Profile[notification.body.senderData.sender].AllPrice = 0;
                                                                while (await reader.ReadAsync()) // построчно считываем данные
                                                                {
                                                                    Profile[notification.body.senderData.sender].ItemsSet.Add(
                                                                        new ItemSetStruct
                                                                        {
                                                                            //     ID = reader.GetInt32("ItemID"),
                                                                            // Include = reader.GetValue("Include").ToString(),
                                                                            //     Weight = reader.GetValue("Weight"),
                                                                            Price = reader.GetValue("Price"),
                                                                            //    ImageUrl = reader.GetValue("ImageUrl"),
                                                                            Name = reader.GetValue("Name"),
                                                                            Count = reader.GetValue("Count"),
                                                                            // LastQuery = LastQuery.None,
                                                                            //   IsOrder = 1
                                                                        });
                                                                    Profile[notification.body.senderData.sender].AllPrice += reader.GetInt32("Price") * reader.GetInt32("Count");
                                                                }


                                                                for (int i = 0; i < Profile[notification.body.senderData.sender].ItemsSet.Count; i++)
                                                                {
                                                                    Profile[notification.body.senderData.sender].PartOrderText =
                                                                        $"\n{i + 1}) {Profile[notification.body.senderData.sender].ItemsSet[i].Name} - {Profile[notification.body.senderData.sender].ItemsSet[i].Count} шт. = {Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[i].Price) * Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[i].Count)} ₽";


                                                                }
                                                            }

                                                            if (Profile[notification.body.senderData.sender].DeliveryInfo.DeliveryType == "🚚 Заказать доставку 🚚")
                                                            {
                                                                Profile[notification.body.senderData.sender].AllPrice += 150;
                                                            }
                                                        }
                                                    }
                                                    Profile[notification.body.senderData.sender].TimelyCaptionOrder = "🛍️ Данные заказа:" +
                                                        $"\n\nСумма: {Profile[notification.body.senderData.sender].AllPrice} ₽" +
                                                        $"\nПокупатель: {Profile[notification.body.senderData.sender].DeliveryInfo.Buyer}" +
                                                        $"\nТелефон: {Profile[notification.body.senderData.sender].DeliveryInfo.Phone}";

                                                    Profile[notification.body.senderData.sender].TimelyCaptionOrder += $"\n\nТовары:{Profile[notification.body.senderData.sender].PartOrderText}";
                                                    Profile[notification.body.senderData.sender].TimelyCaptionOrder += $"\n\nДоставка: {Profile[notification.body.senderData.sender].DeliveryInfo.DeliveryType} " +
                                                        $"{Profile[notification.body.senderData.sender].DeliveryInfo.Address} {Profile[notification.body.senderData.sender].DeliveryInfo.Time}" +
                                                        $"\n\n1 - Подтвердить и отправить" +
                                                        $"\n2 - Изменить" +
                                                        $"\n3 - Главное меню";

                                                    await SendMessageRequest(Profile[notification.body.senderData.sender].TimelyCaptionOrder, thatNumber);
                                                }
                                                else
                                                {
                                                    await SendMessageRequest(UnkwonCommand, thatNumber);
                                                }


                                                break;

                                            case TypeCommand.AttemptOrder:
                                                if (commandNumber >= 1 && commandNumber <= 3)
                                                {


                                                    switch (commandNumber)
                                                    {
                                                        case 1:

                                                            using (SqlConnection connection = new SqlConnection(PathConnection))
                                                            {
                                                                try
                                                                {
                                                                    await connection.OpenAsync();
                                                                }
                                                                catch { }

                                                                SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Orders", connection);


                                                                Profile[notification.body.senderData.sender].TimelyCaptionOrder = $"🛍️ Заказ #{(int)command.ExecuteScalar() + 1}" +
                                                                    $"\n\nДата: {DateTime.Now.AddHours(6)}" +
                                                       $"\nСумма: {Profile[notification.body.senderData.sender].AllPrice} ₽" +
                                                       $"\nПокупатель: {Profile[notification.body.senderData.sender].DeliveryInfo.Buyer}" +
                                                       $"\nТелефон: {Profile[notification.body.senderData.sender].DeliveryInfo.Phone}";

                                                                Profile[notification.body.senderData.sender].TimelyCaptionOrder += $"\n\nТовары:{Profile[notification.body.senderData.sender].PartOrderText}";
                                                                Profile[notification.body.senderData.sender].TimelyCaptionOrder += $"\n\nДоставка: {Profile[notification.body.senderData.sender].DeliveryInfo.DeliveryType} " +
                                                                    $"{Profile[notification.body.senderData.sender].DeliveryInfo.Address} {Profile[notification.body.senderData.sender].DeliveryInfo.Time}";

                                                     //           await RequestNonResponceToDB($"INSERT INTO Orders (Caption, AccountPhone) VALUES (N'{Profile[notification.body.senderData.sender].TimelyCaptionOrder}'," +
                                                       //             $"{GetValidPhone(notification.body.senderData.sender)}");
                                                                await SendMessageRequest(Profile[notification.body.senderData.sender].TimelyCaptionOrder, thatNumber);
                                                                await SendMessageRequest(SoonCallOperator, thatNumber);

                                                                await StartPanel(thatNumber);
                                                            }
                                                            break;

                                                        case 2:
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.TypeDelivery;
                                                            await SendMessageRequest("Выберите вариант доставки:" +
                                                                "\n\n1 - 🚚 Заказать доставку 🚚 (Стоимость доставки - 150 ₽)\n2 - 🙋🏻‍♂️ Заберу сам 🙋🏻‍♀️(Стоимость доставки - 0 ₽)"
                                                                , thatNumber);
                                                            break;

                                                        case 3:
                                                            await StartPanel(thatNumber);
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    await SendMessageRequest("Выбран неизвестный вариант", thatNumber);
                                                }
                                                break;

                                            case TypeCommand.ChangeOrderCount:
                                             //   await RequestNonResponceToDB($"UPDATE Notes SET Count = {Profile[notification.body.senderData.sender].TextNotification} WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)} AND ItemID = {Profile[notification.body.senderData.sender].NowBuyItemID}");
                                                // OperationalProfile[notification.body.senderData.sender].ItemsSet[OperationalProfile[notification.body.senderData.sender].SendItemMenuNumber - 1].Count = OperationalProfile[notification.body.senderData.sender].TextNotification;

                                                await SendMessageRequest("Корзина обновлена", thatNumber);

                                                //Вычитаю старую
                                                Profile[notification.body.senderData.sender].AllPrice -=
                                                    Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Count) *
                                                    Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Price);

                                                //Изменяю в списке
                                                Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Count = Profile[notification.body.senderData.sender].TextNotification;

                                                //Прибавляю новою
                                                Profile[notification.body.senderData.sender].AllPrice +=
                                                   Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Count) *
                                                   Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Price);



                                                Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Caption =
                          $"{Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Name}" +
                          $"\nВес: {Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Weight} кг" +
                          $"\n\n{Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Include}" +
                          $"\n\n{Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Price}₽ * {Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Count} шт. " +
                          $"= {Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Price) * Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Count)} ₽" +

                          $"\n\n1 - Удалить из корзины" +
                          $"\n2 - Изменить кол-во" +
                          $"{((Profile[notification.body.senderData.sender].SendItemMenuNumber - 1) == Profile[notification.body.senderData.sender].ItemsSet.Count - 1 ? "" : "\n3 - Показать ещё")}" +
                          $"\n\n4 - Заказ на {Profile[notification.body.senderData.sender].AllPrice}₽ Оформить?" +
                          $"\n5 - Продолжить покупки";

                                                await SendMessageUrlRequest(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Caption,
                          $"set{Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].ID}.png", Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].ImageUrl.ToString(), thatNumber);

                                                break;
                                        }
                                    }

                                    //Если слово
                                    else
                                    {
                                        if (Profile[notification.body.senderData.sender].typeCommand == TypeCommand.Settings_Name)
                                        {
                                            await UpsertToDB($"INSERT INTO Accounts (Name, Phone) VALUES(N'{notification.body.messageData.textMessageData.textMessage}'," +
                                                   $"{GetValidPhone(notification.body.senderData.sender)})",
                                                   $"UPDATE Accounts SET Name = N'{notification.body.messageData.textMessageData.textMessage}' WHERE Phone = {GetValidPhone(notification.body.senderData.sender)}", $"SELECT * FROM Accounts WHERE Phone = {GetValidPhone(notification.body.senderData.sender)}");
                                            await SendMessageRequest("🙋🏻‍♂️ Имя изменено", thatNumber);
                                        }
                                        else if (Profile[notification.body.senderData.sender].typeCommand == TypeCommand.Settings_Birthday)
                                        {
                                            if (DateTime.TryParse(notification.body.messageData.textMessageData.textMessage, out var birthday))
                                            {
                                                await UpsertToDB($"INSERT INTO Accounts (Birthday, Phone) VALUES('{birthday.ToString("yyyy.MM.dd")}'," +
                                                       $"{GetValidPhone(notification.body.senderData.sender)})",
                                                       $"UPDATE Accounts SET Birthday = '{birthday.ToString("yyyy.MM.dd")}' WHERE Phone = {GetValidPhone(notification.body.senderData.sender)}", $"SELECT * FROM Accounts WHERE Phone = {GetValidPhone(notification.body.senderData.sender)}");
                                                await SendMessageRequest("📅 День рождения изменён", thatNumber);
                                                await StartPanel(thatNumber);
                                            }
                                            else
                                            {
                                                await SendMessageRequest("❌ Неправильный формат даты. Попробуйте снова", thatNumber);
                                            }

                                        }
                                        else if (Profile[notification.body.senderData.sender].typeCommand == TypeCommand.Settings_Address)
                                        {

                                            await UpsertToDB($"INSERT INTO Accounts (Address, Phone) VALUES('{notification.body.messageData.textMessageData.textMessage}'," +
                                                   $"{GetValidPhone(notification.body.senderData.sender)})",
                                                   $"UPDATE Accounts SET Address = '{notification.body.messageData.textMessageData.textMessage}' WHERE Phone = {GetValidPhone(notification.body.senderData.sender)}", $"SELECT * FROM Accounts WHERE Phone = {GetValidPhone(notification.body.senderData.sender)}");
                                            await SendMessageRequest("🚩 Адрес изменён", thatNumber);
                                            await StartPanel(thatNumber);



                                        }
                                        else if (Profile[notification.body.senderData.sender].typeCommand == TypeCommand.TimeDelivery)
                                        {



                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.NameDelivery;
                                            Profile[notification.body.senderData.sender].DeliveryInfo.Time = Profile[notification.body.senderData.sender].TextNotification;
                                            await SendMessageRequest($"Укажите Ваше имя:\r\nСейчас: {Profile[notification.body.senderData.sender].AccountInfo.Name}" +
                                                $"{(Profile[notification.body.senderData.sender].AccountInfo.Name == "Не указано" ? "" : "\n\n1 - Оставить как есть\nИли введите другое")}", thatNumber);


                                        }
                                        else if (Profile[notification.body.senderData.sender].typeCommand == TypeCommand.AddressDelivery)
                                        {
                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.TimeDelivery;
                                            Profile[notification.body.senderData.sender].DeliveryInfo.Address = Profile[notification.body.senderData.sender].TextNotification;

                                            await SendMessageRequest("🕓 Укажите к какому времени приготовить заказ:\r\nСейчас: как можно скорее" +
                                                "\n\n1 - Оставить как есть\nИли введите другое", thatNumber);

                                        }
                                        else if (Profile[notification.body.senderData.sender].typeCommand == TypeCommand.NameDelivery)
                                        {
                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.PhoneDelivery;
                                            Profile[notification.body.senderData.sender].DeliveryInfo.Buyer = Profile[notification.body.senderData.sender].TextNotification;
                                            await SendMessageRequest($"Укажите Номер телефона:\r\nСейчас: {Profile[notification.body.senderData.sender].AccountInfo.Phone}" +
                                                $"{(Profile[notification.body.senderData.sender].AccountInfo.Phone == "Не указан" ? "" : "\n\n1 - Оставить как есть\nИли введите другой")}", thatNumber);

                                        }
                                        else if (Profile[notification.body.senderData.sender].typeCommand == TypeCommand.PhoneDelivery)
                                        {
                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.AttemptOrder;
                                            Profile[notification.body.senderData.sender].DeliveryInfo.Phone = Profile[notification.body.senderData.sender].TextNotification;

                                            using (SqlConnection connection = new SqlConnection(PathConnection))
                                            {
                                                //Открываем соединение
                                                try
                                                {
                                                    await connection.OpenAsync();
                                                }
                                                catch { }

                                                SqlCommand command1 = new SqlCommand($"SELECT * FROM Notes JOIN Sets ON Notes.ItemID = Sets.Id WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)}" +
                                                    $" AND IsOrder = 1", connection);

                                                //Спец список для временного хранения полученных сетов, чтобы потом понять, какой из них переслали
                                                if (Profile[notification.body.senderData.sender].ItemsSet.Count > 0)
                                                {
                                                    Profile[notification.body.senderData.sender].ItemsSet.Clear();
                                                    Profile[notification.body.senderData.sender].PartOrderText = string.Empty;
                                                    Profile[notification.body.senderData.sender].TimelyCaptionOrder = string.Empty;
                                                }

                                                using (SqlDataReader reader = await command1.ExecuteReaderAsync())
                                                {
                                                    if (reader.HasRows) // если есть данные
                                                    {
                                                        Profile[notification.body.senderData.sender].AllPrice = 0;
                                                        while (await reader.ReadAsync()) // построчно считываем данные
                                                        {
                                                            Profile[notification.body.senderData.sender].ItemsSet.Add(
                                                                new ItemSetStruct
                                                                {
                                                                    Price = reader.GetValue("Price"),
                                                                    Name = reader.GetValue("Name"),
                                                                    Count = reader.GetValue("Count"),
                                                                });
                                                            Profile[notification.body.senderData.sender].AllPrice += reader.GetInt32("Price") * reader.GetInt32("Count");
                                                        }

                                                        for (int i = 0; i < Profile[notification.body.senderData.sender].ItemsSet.Count; i++)
                                                        {
                                                            Profile[notification.body.senderData.sender].PartOrderText =
                                                                $"\n{i + 1}) {Profile[notification.body.senderData.sender].ItemsSet[i].Name} - {Profile[notification.body.senderData.sender].ItemsSet[i].Count} шт. = {Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[i].Price) * Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[i].Count)} ₽";


                                                        }
                                                    }
                                                    if (Profile[notification.body.senderData.sender].DeliveryInfo.DeliveryType == "🚚 Заказать доставку 🚚")
                                                    {
                                                        Profile[notification.body.senderData.sender].AllPrice += 150;
                                                    }
                                                }
                                            }
                                            Profile[notification.body.senderData.sender].TimelyCaptionOrder = "🛍️ Данные заказа:" +
                                                $"\n\nСумма: {Profile[notification.body.senderData.sender].AllPrice} ₽" +
                                                $"\nПокупатель: {Profile[notification.body.senderData.sender].DeliveryInfo.Buyer}" +
                                                $"\nТелефон: {Profile[notification.body.senderData.sender].DeliveryInfo.Phone}";

                                            Profile[notification.body.senderData.sender].TimelyCaptionOrder += $"\n\nТовары:{Profile[notification.body.senderData.sender].PartOrderText}";
                                            Profile[notification.body.senderData.sender].TimelyCaptionOrder += $"\n\nДоставка: {Profile[notification.body.senderData.sender].DeliveryInfo.DeliveryType} " +
                                                    $"{Profile[notification.body.senderData.sender].DeliveryInfo.Address} {Profile[notification.body.senderData.sender].DeliveryInfo.Time}" +
                                                $"\n\n1 - Подтвердить и отправить" +
                                                $"\n2 - Изменить" +
                                                $"\n3 - Главное меню";

                                            await SendMessageRequest(Profile[notification.body.senderData.sender].TimelyCaptionOrder, thatNumber);

                                        }

                                        string text = Profile[notification.body.senderData.sender].TextNotification.Trim().ToLower();

                                        switch (text)
                                        {
                                            case "/старт":
                                                await StartPanel(thatNumber); break;


                                            case "/помощь":
                                                await SendMessageRequest(CommandsList, thatNumber);
                                                Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Help; break;
                                                break;

                                            case "/корзина":
                                                CartPanel(notification, thatNumber);
                                                break;

                                            case "/история":
                                                GetOdersHistory(thatNumber);
                                                break;

                                            case "/меню":
                                                GetMenu(notification, true, notification.body.senderData.sender);
                                                break;

                                            case "/новости":
                                                Profile[notification.body.senderData.sender].LastDate = null;
                                                GetNew(notification.body.senderData.sender);

                                                Profile[notification.body.senderData.sender].typeCommand = TypeCommand.News;
                                                break;

                                            case "/настройки":
                                                await SendMessageRequest(SettingsTextCommands, thatNumber);
                                                Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Settings; break;

                                            default:
                                                if (Profile[notification.body.senderData.sender].typeCommand != TypeCommand.NameDelivery &&
                                                    Profile[notification.body.senderData.sender].typeCommand != TypeCommand.PhoneDelivery &&
                                                     Profile[notification.body.senderData.sender].typeCommand != TypeCommand.AttemptOrder &&
                                                      Profile[notification.body.senderData.sender].typeCommand != TypeCommand.TimeDelivery)
                                                {

                                                    await SendMessageRequest(UnkwonCommand, thatNumber);
                                                }
                                                break;
                                        }
                                    }
                                }

                                //Если выключен
                                else
                                {
                                    await SendMessageRequest(TimelyOff, thatNumber);
                                }
                            }

                            //Значит написал админ
                            else
                            {
                                if (notification.body.messageData.typeMessage == "textMessage")
                                {
                                    Profile[notification.body.senderData.sender].TextNotification = notification.body.messageData.textMessageData.textMessage;
                                }
                                else if (notification.body.messageData.typeMessage == "extendedTextMessage" || notification.body.messageData.typeMessage == "" +
                                    "quotedMessage")
                                {
                                    Profile[notification.body.senderData.sender].TextNotification = notification.body.messageData.extendedTextMessageData.text;
                                }

                                if (Profile[notification.body.senderData.sender].typeCommand == TypeCommand.Start) { await StartAdminPanel(); continue; }

                                if (int.TryParse(Profile[notification.body.senderData.sender].TextNotification, out int commandNumber) ||
                                     Profile[notification.body.senderData.sender].typeCommand == TypeCommand.CreateNew ||
                                     Profile[notification.body.senderData.sender].typeCommand == TypeCommand.CreateNew_Caption ||
                                     Profile[notification.body.senderData.sender].typeCommand == TypeCommand.CreateNew_Image ||

                                      Profile[notification.body.senderData.sender].typeCommand == TypeCommand.NewSet_Caption ||
                                          Profile[notification.body.senderData.sender].typeCommand == TypeCommand.NewSet ||
                                     Profile[notification.body.senderData.sender].typeCommand == TypeCommand.NewSet_ImageUrl ||
                                     Profile[notification.body.senderData.sender].typeCommand == TypeCommand.NewSet_Price ||
                                      Profile[notification.body.senderData.sender].typeCommand == TypeCommand.NewSet_Weigth)
                                {
                                    switch (Profile[notification.body.senderData.sender].typeCommand)
                                    {
                                        case TypeCommand.MainMenu:
                                            {
                                                if (commandNumber >= 1 && commandNumber <= 4)
                                                {
                                                    switch (commandNumber)
                                                    {
                                                        case 1:
                                                            if (NowOn == true) { NowOn = false; await SendMessageRequest("❌ Магазин выключен", notification.body.senderData.sender); }
                                                            else if (NowOn == false) { NowOn = true; await SendMessageRequest("✅ Магазин включен", notification.body.senderData.sender); }
                                                            break;

                                                        case 2:
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.CreateNew;
                                                            await SendMessageRequest("🚩 Введите название новости", MyOwnNumber);
                                                            break;

                                                        case 3:
                                                            GetMenu(notification, true, MyOwnNumber);
                                                            break;

                                                        case 4:
                                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.NewSet;
                                                            await SendMessageRequest("🍽️ Введите название товара", MyOwnNumber);
                                                            break;
                                                    }
                                                }
                                            }
                                            break;

                                        case TypeCommand.NewSet:
                                            CreatingSet.Name = Profile[notification.body.senderData.sender].TextNotification;
                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.NewSet_Caption;
                                            await SendMessageRequest("🍽️ Введите описание товара", MyOwnNumber);
                                            break;

                                        case TypeCommand.NewSet_Caption:
                                            CreatingSet.Caption = Profile[notification.body.senderData.sender].TextNotification;
                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.NewSet_Weigth;
                                            await SendMessageRequest("🍽️ Введите вес товара (кг)", MyOwnNumber);
                                            break;

                                        case TypeCommand.NewSet_Weigth:
                                            CreatingSet.Weight = Profile[notification.body.senderData.sender].TextNotification;
                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.NewSet_Price;
                                            await SendMessageRequest("🍽️ Введите цену товара", MyOwnNumber);
                                            break;

                                        case TypeCommand.Help:
                                            if (commandNumber == 0)
                                            {
                                                switch (commandNumber)
                                                {
                                                    case 0:

                                                        await StartAdminPanel();
                                                        break;
                                                }
                                            }
                                            break;

                                        case TypeCommand.NewSet_Price:
                                            CreatingSet.Price = Profile[notification.body.senderData.sender].TextNotification;
                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.NewSet_ImageUrl;
                                            await SendMessageRequest("🖼️ Загрузите изображение", MyOwnNumber);
                                            break;

                                        case TypeCommand.NewSet_ImageUrl:
                                            if (notification.body.messageData.typeMessage == "imageMessage")
                                            {
                                                CreatingSet.ImageUrl = await UploadToIngBB(AdminImageUrl);

                                          //      await RequestNonResponceToDB("INSERT INTO Sets (Name, Include, Weight, Price, ImageUrl) VALUES " +
                                            //  $"(N'{CreatingSet.Name}', N'{CreatingSet.Caption}', N'{CreatingSet.Weight}', N'{CreatingSet.Price}', N'{CreatingSet.ImageUrl}')");
                                                await SendMessageRequest("✅ Сет добавлен", MyOwnNumber);

                                                await StartAdminPanel();
                                            }
                                            else
                                            {
                                                await SendMessageRequest(UnkwonCommand, notification.body.senderData.sender);
                                            }
                                            break;

                                        case TypeCommand.CreateNew:

                                            CreatingNew.Name = Profile[notification.body.senderData.sender].TextNotification;
                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.CreateNew_Caption;
                                            await SendMessageRequest("🚩 Введите описание новости", MyOwnNumber);

                                            break;

                                        case TypeCommand.CreateNew_Caption:

                                            CreatingNew.Caption = Profile[notification.body.senderData.sender].TextNotification;
                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.CreateNew_Image;
                                            await SendMessageRequest("🖼️ Загрузите изображение. Если его нет, напишите \"Нет\"", MyOwnNumber);
                                            break;

                                        case TypeCommand.CreateNew_Image:

                                            if (Profile[notification.body.senderData.sender].TextNotification.Trim().ToLower() == "нет")
                                            {
                                                CreatingNew.ImageUrl = null;

                                                await RequestNonResponceToDB(@"
DECLARE $name AS Utf8;
DECLARE $date AS Date;
DECLARE $description AS Utf8;
DECLARE $id AS Uint64;

UPSERT INTO News (ID, Name, Date, Description) VALUES ($id,$name, $date, $description)", new Dictionary<string, YdbValue>
                                                {
                                                    {"$name", YdbValue.MakeUtf8(CreatingNew.Name) },
                                                     {"$date", YdbValue.MakeDate(DateTime.Now.AddHours(6)) },
                                                      {"$description", YdbValue.MakeUtf8(CreatingNew.Caption) },
                                                      {"$id", YdbValue.MakeUint64(Convert.ToUInt64(DateTime.Now.ToString("yyyyMMddHHmmssff"))) },
                                                });
                                                await SendMessageRequest("✅ Новость добавлена", MyOwnNumber);

                                                await StartAdminPanel();
                                            }
                                            else if (notification.body.messageData.typeMessage == "imageMessage")
                                            {
                                                CreatingNew.ImageUrl = await UploadToIngBB(AdminImageUrl);

                                             //   await RequestNonResponceToDB("INSERT INTO News (Name, Date, Description, ImageUrl) VALUES " +
                                              // $"(N'{CreatingNew.Name}', '{DateTime.Now.AddHours(6).ToString("yyyy-MM-dd")}', N'{CreatingNew.Caption}', '{CreatingNew.ImageUrl}')");
                                                await SendMessageRequest("✅ Новость добавлена", MyOwnNumber);

                                                await StartAdminPanel();
                                            }
                                            else
                                            {
                                                await SendMessageRequest(UnkwonCommand, notification.body.senderData.sender);
                                            }

                                            break;

                                        case TypeCommand.SelectSet:
                                            if (commandNumber >= 1 && commandNumber <= 2)
                                            {
                                                if (notification.body.typeWebhook == "quotedMessage")
                                                {
                                                    Profile[notification.body.senderData.sender].NowBuyPrice = Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.Caption == notification.body.messageData.quotedMessage.caption).First().Price as int?;
                                                    Profile[notification.body.senderData.sender].NowBuyItemID = Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.Caption == notification.body.messageData.quotedMessage.caption).First().ID;

                                                }
                                                else
                                                {
                                                    Profile[notification.body.senderData.sender].NowBuyPrice = Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].Price as int?;
                                                    Profile[notification.body.senderData.sender].NowBuyItemID = Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].ID;
                                                }

                                                switch (commandNumber)
                                                {
                                                    case 1:
                                                 //       await RequestNonResponceToDB($"DELETE Sets WHERE Id = {Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber - 1].ID}");

                                                        await SendMessageRequest("🗑️ Сет удалён", MyOwnNumber);
                                                        break;

                                                    case 2:

                                                        if (Profile[notification.body.senderData.sender].SendItemMenuNumber >= Profile[notification.body.senderData.sender].ItemsSet.Count)
                                                        {
                                                            await SendMessageRequest("❌ К сожалению, больше нет", MyOwnNumber);
                                                        }
                                                        else
                                                        {
                                                            await SendMessageUrlRequest(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].Caption,
                                                                           $"set{Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ID}.png", Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ImageUrl.ToString(), MyOwnNumber);

                                                            Profile[notification.body.senderData.sender].SendItemMenuNumber++;
                                                        }
                                                        break;

                                                }


                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    string text = Profile[notification.body.senderData.sender].TextNotification.Trim().ToLower();

                                    switch (text)
                                    {
                                        case "/старт":
                                            await StartAdminPanel(); break;


                                        case "/помощь":
                                            await SendMessageRequest(AdminCommandsList, MyOwnNumber);
                                            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Help; break;

                                        case "/меню":
                                            GetMenu(notification, true, MyOwnNumber);
                                            break;

                                        default:

                                            await SendMessageRequest(UnkwonCommand, MyOwnNumber);
                                            break;
                                    }
                                }

                            }
                        }
                    }
                }

            }
        }

        private async static Task StartPanel(string thatNumber)
        {
            await SendMessageRequest(MainTxt, thatNumber);
            Profile[thatNumber].typeCommand = TypeCommand.MainMenu;
        }
        private async static Task StartAdminPanel()
        {
            string greece = AdminPanel.Insert(0, (NowOn == true ? "Здравствуйте, выберите команду:\n\n❌ 1 - Выключить магазин" : "Здравствуйте, выберите команду:\n\n✅ 1 - Включить магазин"));
            await SendMessageRequest(greece, notification.body.senderData.sender);
            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.MainMenu;
        }
        private async static Task<string> UploadToIngBB(string url)
        {
            var Response = await new HttpClient().PostAsync($"https://api.imgbb.com/1/upload?image={url}&key=bbebc63abe564077de07bcbf46e2bf1e", null);
            //   string g =;

            return (JsonConvert.DeserializeObject<ImgResponce>(await Response?.Content.ReadAsStringAsync()) as ImgResponce).data.url;
        }
        private async static void CartPanel(StructGettingNotification notification, string thatNumber)
        {
            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.Cart;
            Profile[notification.body.senderData.sender].SendItemMenuNumber = 0;

            //Получаем сырые данные сетов(без оболочки)
            using (SqlConnection connection = new SqlConnection(PathConnection))
            {
                //Открываем соединение
                try
                {
                    await connection.OpenAsync();
                }
                catch { }

                SqlCommand command1 = new SqlCommand($"SELECT * FROM Notes JOIN Sets ON Notes.ItemID = Sets.Id WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)}" +
                    $" AND IsOrder = 1", connection);

                //Спец список для временного хранения полученных сетов, чтобы потом понять, какой из них переслали
                if (Profile[notification.body.senderData.sender].ItemsSet.Count > 0)
                {
                    Profile[notification.body.senderData.sender].ItemsSet.Clear();
                }

                using (SqlDataReader reader = await command1.ExecuteReaderAsync())
                {
                    if (reader.HasRows) // если есть данные
                    {
                        Profile[notification.body.senderData.sender].AllPrice = 0;
                        while (await reader.ReadAsync()) // построчно считываем данные
                        {
                            Profile[notification.body.senderData.sender].ItemsSet.Add(
                                new ItemSetStruct
                                {
                                    //ID = reader.GetInt32("ItemID"),
                                    Include = reader.GetValue("Include").ToString(),
                                    Weight = reader.GetValue("Weight"),
                                    Price = reader.GetValue("Price"),
                                    ImageUrl = reader.GetValue("ImageUrl"),
                                    Name = reader.GetValue("Name"),
                                    Count = reader.GetValue("Count"),
                                    // LastQuery = LastQuery.None,
                                    IsOrder = true
                                });
                            Profile[notification.body.senderData.sender].AllPrice += reader.GetInt32("Price") * reader.GetInt32("Count");
                        }

                        for (int i = 0; i < Profile[notification.body.senderData.sender].ItemsSet.Count; i++)
                        {
                            Profile[notification.body.senderData.sender].ItemsSet[i].Caption =
                           $"{Profile[notification.body.senderData.sender].ItemsSet[i].Name}" +
                           $"\nВес: {Profile[notification.body.senderData.sender].ItemsSet[i].Weight} кг" +
                           $"\n\n{Profile[notification.body.senderData.sender].ItemsSet[i].Include}" +
                           $"\n\n{Profile[notification.body.senderData.sender].ItemsSet[i].Price}₽ * {Profile[notification.body.senderData.sender].ItemsSet[i].Count} шт. " +
                           $"= {Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[i].Price) * Convert.ToDouble(Profile[notification.body.senderData.sender].ItemsSet[i].Count)} ₽" +

                           $"\n\n🗑️ 1 - Удалить из корзины" +
                           $"\n✏️ 2 - Изменить кол-во" +
                           $"{(i == Profile[notification.body.senderData.sender].ItemsSet.Count - 1 ? "" : "\n💎 3 - Показать ещё")}" +
                           $"\n\n✅ 4 - Заказ на {Profile[notification.body.senderData.sender].AllPrice}₽ Оформить?" +
                           $"\n🛍️5 - Продолжить покупки";
                        }

                        //Отправляем первый сет
                        await SendMessageUrlRequest(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].Caption,
                           $"set{Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ID}.png", Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ImageUrl.ToString(), thatNumber);

                        //Увеличиваем порядковый номер отправленного сета
                        Profile[notification.body.senderData.sender].SendItemMenuNumber++;
                    }
                    else
                    {
                        await SendMessageRequest(EmptyCart, thatNumber);
                    }
                }
                //Закрыли соединение
            }
        }
        private static string GetValidPhone(string phone)
        {
            return phone.TrimEnd("@c.us".ToCharArray());
        }
        private async static Task<List<object>> RequestToDB(string query)
        {
            using (SqlConnection connection = new SqlConnection(PathConnection))
            {
                //Открываем соединение
                try
                {
                    await connection.OpenAsync();
                }
                catch { }

                SqlCommand command1 = new SqlCommand(query, connection);

                using (SqlDataReader reader = await command1.ExecuteReaderAsync())
                {
                    int i = 0;
                    List<object> response = new List<object>();

                    if (reader.HasRows) // если есть данные
                    {
                        while (await reader.ReadAsync()) // построчно считываем данные
                        {
                            response.Add(reader.GetValue(i));
                            i++;

                        }
                    }
                    return response;
                }
            }
        }

        private async static Task UpsertToDB(string insert_query, string update_query, string select_query)
        {
            using (SqlConnection connection = new SqlConnection(PathConnection))
            {
                try
                {
                    await connection.OpenAsync();
                }
                catch { }

                SqlCommand command = new SqlCommand(select_query, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {

                    SqlConnection connection1 = new SqlConnection(PathConnection);

                    if (reader.HasRows) // если есть данные
                    {

                        await connection1.OpenAsync();
                        SqlCommand update_command = new SqlCommand(update_query, connection1);
                        int i = await update_command.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        await connection.CloseAsync();
                        await connection1.OpenAsync();
                        SqlCommand insert_command = new SqlCommand(insert_query, connection1);
                        int i = await insert_command.ExecuteNonQueryAsync();
                    }
                }
            }
        }
        private async static Task RequestNonResponceToDB(string update_query, Dictionary<string, YdbValue> dictionary)
        {


            var response = await VyshkaDictionary.tableClient.SessionExec(async session =>
            {
                return await session.ExecuteDataQuery(
                query: update_query,
                    parameters: dictionary,
                    txControl: TxControl.BeginSerializableRW().Commit()
                );
            });

            response.Status.EnsureSuccess();

           



        }
        private async static void GetMenu(StructGettingNotification notification, bool IsOrder, string thatNumber)
        {
            var response = await VyshkaDictionary.tableClient.SessionExec(async session =>
            {
                return await session.ExecuteDataQuery(
                query: "SELECT * FROM Sets",
                    parameters: new Dictionary<string, YdbValue>(),
                    txControl: TxControl.BeginSerializableRW().Commit()
                );
            });

            response.Status.EnsureSuccess();

            var queryResponse = (ExecuteDataQueryResponse)response;

            Profile[notification.body.senderData.sender].ResultSets = queryResponse.Result.ResultSets;
            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.SelectSet;
            Profile[notification.body.senderData.sender].SendItemMenuNumber = 0;
            Profile[notification.body.senderData.sender].IsOrder = IsOrder;

            //Получаем сырые данные сетов(без оболочки)

            
            //Спец список для временного хранения полученных сетов, чтобы потом понять, какой из них переслали
            //if (Profile[notification.body.senderData.sender].ItemsSet.Count > 0)
            //{
            //    Profile[notification.body.senderData.sender].ItemsSet.Clear();
            //}


            foreach (ResultSet.Row row in Profile[notification.body.senderData.sender].ResultSets[0].Rows)
            {
                Profile[notification.body.senderData.sender].ItemsSet.Add(
                                new ItemSetStruct
                                {
                                    ID = (ulong)row["ID"].GetUint64(),
                                    Include = (string?)row["Include"].GetOptionalUtf8(),
                                    Weight = (double)row["Weight"].GetOptionalDouble(),
                                    Price = (double)row["Price"].GetOptionalDouble(),
                                    ImageUrl = (string?)row["ImageUrl"].GetOptionalUtf8(),
                                    Name = (string?)row["Name"].GetOptionalUtf8(),
                                    LastQuery = LastQuery.None
                                });
            }





            //Закрыли соединение


            //Создание Caption для каждого
            for (int i = 0; i < Profile[notification.body.senderData.sender].ItemsSet.Count; i++)
            {
                Profile[notification.body.senderData.sender].ItemsSet[i].Caption =
               $"{Profile[notification.body.senderData.sender].ItemsSet[i].Name}" +
               $"\nВес: {Profile[notification.body.senderData.sender].ItemsSet[i].Weight} кг" +
               $"\n\n{Profile[notification.body.senderData.sender].ItemsSet[i].Include}";

                if (notification.body.senderData.sender == MyOwnNumber)
                {
                    Profile[notification.body.senderData.sender].ItemsSet[i].Caption +=
                    $"\n\n1 - 🗑️ Удалить" + $"{(i == Profile[notification.body.senderData.sender].ItemsSet.Count - 1 ? "" : "\n 2 - 💎Показать ещё")}";
                }
                else
                {
                    Profile[notification.body.senderData.sender].ItemsSet[i].Caption +=
                     $"\n\n1 - Купить за {Profile[notification.body.senderData.sender].ItemsSet[i].Price}₽" +
               $"\n2 - ⭐ Добавить в избранное" +
               $"{(i == Profile[notification.body.senderData.sender].ItemsSet.Count - 1 ? "" : "\n 3 - 💎 Показать ещё")}";

                }
            }


            if (!IsOrder)
            {
                var response1 = await VyshkaDictionary.tableClient.SessionExec(async session =>
                {
                    return await session.ExecuteDataQuery(
                    query: $"SELECT * FROM Notes WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)} AND IsFavorite = 1",
                        parameters: new Dictionary<string, YdbValue>(),
                        txControl: TxControl.BeginSerializableRW().Commit()
                    );
                });

                response.Status.EnsureSuccess();

                var queryResponse1 = (ExecuteDataQueryResponse)response1;

                Profile[notification.body.senderData.sender].ResultSets = queryResponse1.Result.ResultSets;

            }
            else
            {
                var response1 = await VyshkaDictionary.tableClient.SessionExec(async session =>
                {
                return await session.ExecuteDataQuery(
                query: @"
                    DECLARE $phone as Utf8;
                    SELECT * FROM Notes WHERE Phone = $phone",
                    parameters: new Dictionary<string, YdbValue>
                    {
                            { "$phone", YdbValue.MakeUtf8(GetValidPhone(notification.body.senderData.sender))},

                    },
                    txControl: TxControl.BeginSerializableRW().Commit());
                });

                response1.Status.EnsureSuccess();

                var queryResponse1 = (ExecuteDataQueryResponse)response1;

                Profile[notification.body.senderData.sender].ResultSets = queryResponse1.Result.ResultSets;
            }

            foreach (ResultSet.Row row in Profile[notification.body.senderData.sender].ResultSets[0].Rows)
            {
                //Обновить данные в локальном списке после взятия из бд
                Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.ID == (ulong)row["ItemID"]).First().Caption =
                                $"{Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.ID == (ulong)row["ItemID"]).First().Name}" +
                                $"\nВес: {Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.ID == (ulong)row["ItemID"]).First().Weight} кг" +
                                $"\n\n{Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.ID == (ulong)row["ItemID"]).First().Include}" +
                                $"\n\n1 - Купить за {Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.ID == (ulong)row["ItemID"]).First().Price}₽" +
                                $"\n2 - {((bool)row["IsFavorite"] == true ? "⭐Удалить из избранного" : "⭐Добавить в избранное")}" +

                                $"{(Profile[notification.body.senderData.sender].ItemsSet.IndexOf(Profile[notification.body.senderData.sender].ItemsSet.First(x => x.ID == (ulong)row["ItemID"])) == Profile[notification.body.senderData.sender].ItemsSet.Count - 1 ? "" : "\n3 - Посмотреть ещё")}" +
                                $"{((int)row["Count"] == 1 ? "" : $"\n\nВыбранное кол-во: {(int)row["Count"]} шт")}";

                Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.ID == (ulong)row["ItemID"]).First().IsOrder = (bool)row["IsOrder"];
                Profile[notification.body.senderData.sender].ItemsSet.Where(x => x.ID == (ulong)row["ItemID"]).First().IsFavorite = (bool)row["IsFavorite"];

            }

            //Отправляем первый сет
            await SendMessageUrlRequest(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].Caption,
               $"set{Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ID}.png", Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ImageUrl.ToString(), thatNumber);

            //Увеличиваем порядковый номер отправленного сета
            Profile[notification.body.senderData.sender].SendItemMenuNumber++;
        }
        //Закрыли соединение
    

        private async static void GetFavorites(StructGettingNotification notification, string thatNumber)
        {
            Profile[notification.body.senderData.sender].typeCommand = TypeCommand.SelectSet;
            Profile[notification.body.senderData.sender].SendItemMenuNumber = 0;

            //Получаем сырые данные сетов(без оболочки)
            using (SqlConnection connection = new SqlConnection(PathConnection))
            {
                //Открываем соединение
                try
                {
                    await connection.OpenAsync();
                }
                catch { }

                SqlCommand command1 = new SqlCommand($"SELECT * FROM Notes JOIN Sets ON Notes.ItemID = Sets.Id WHERE AccountPhone = {GetValidPhone(notification.body.senderData.sender)}" +
                    $" AND IsFavorite = 1", connection);

                //Спец список для временного хранения полученных сетов, чтобы потом понять, какой из них переслали
                if (Profile[notification.body.senderData.sender].ItemsSet.Count > 0)
                {
                    Profile[notification.body.senderData.sender].ItemsSet.Clear();
                }

                using (SqlDataReader reader = await command1.ExecuteReaderAsync())
                {
                    if (reader.HasRows) // если есть данные
                    {
                        while (await reader.ReadAsync()) // построчно считываем данные
                        {
                            Profile[notification.body.senderData.sender].ItemsSet.Add(
                                new ItemSetStruct
                                {
                                  //  ID = reader.GetInt32("Id"),
                                    Include = reader.GetValue("Include").ToString(),
                                    Weight = reader.GetValue("Weight"),
                                    Price = reader.GetValue("Price"),
                                    ImageUrl = reader.GetValue("ImageUrl"),
                                    Name = reader.GetValue("Name"),
                                    LastQuery = LastQuery.None,
                                    IsFavorite = true
                                });
                        }

                        for (int i = 0; i < Profile[notification.body.senderData.sender].ItemsSet.Count; i++)
                        {
                            Profile[notification.body.senderData.sender].ItemsSet[i].Caption =
                           $"{Profile[notification.body.senderData.sender].ItemsSet[i].Name}" +
                           $"\nВес: {Profile[notification.body.senderData.sender].ItemsSet[i].Weight} кг" +
                           $"\n\n{Profile[notification.body.senderData.sender].ItemsSet[i].Include}" +
                           $"\n\n1 - ✅ Купить за {Profile[notification.body.senderData.sender].ItemsSet[i].Price}₽" +
                           $"\n2 - ⭐Удалить из избранного" +
                           $"{(i == Profile[notification.body.senderData.sender].ItemsSet.Count - 1 ? "" : "\n3 - 💎 Показать ещё")}";
                        }

                        //Отправляем первый сет
                        await SendMessageUrlRequest(Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].Caption,
                           $"set{Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ID}.png", Profile[notification.body.senderData.sender].ItemsSet[Profile[notification.body.senderData.sender].SendItemMenuNumber].ImageUrl.ToString(), thatNumber);

                        //Увеличиваем порядковый номер отправленного сета
                        Profile[notification.body.senderData.sender].SendItemMenuNumber++;
                    }
                    else
                    {
                        await SendMessageRequest("⭐ Вы ещё ничего не добавили в избранное", thatNumber);
                    }
                }
                //Закрыли соединение
            }
         
        }

        private async static void GetNew(string sender)
        {
            if (Profile[sender].LastDate == null) { Profile[sender].LastDate = DateTime.Now; }


            var response1 = await VyshkaDictionary.tableClient.SessionExec(async session =>
            {
                return await session.ExecuteDataQuery(
                query: @" DECLARE $date AS Date;
                    SELECT * FROM News WHERE Date <= $date ORDER BY Date DESC LIMIT 1 ",
                    parameters: new Dictionary<string, YdbValue>
                    {
                            { "$date", YdbValue.MakeDate(Profile[sender].LastDate.Value)},

                    },
                    txControl: TxControl.BeginSerializableRW().Commit());
            });

            while (true)
            {
                try
                {
                    response1.Status.EnsureSuccess();
                    break;
                }
                catch { }
            }
         

            var queryResponse1 = (ExecuteDataQueryResponse)response1;

            //Profile[notification.body.senderData.sender].ResultSets = queryResponse1.Result.ResultSets;



            //SqlCommand command1 = new SqlCommand($"", connection);


            //using (SqlDataReader reader = await command1.ExecuteReaderAsync())
            //{
            //    //если спец записей нет то по умлчанию
            //    if (reader.HasRows) // если есть данные
            //    {
            if (queryResponse1.Result.ResultSets[0].Rows.Count > 0)
            {

                if (queryResponse1.Result.ResultSets[0].Rows[0]["ImageUrl"].GetOptionalUtf8() != null)
                {
                    await SendMessageUrlRequest($"{queryResponse1.Result.ResultSets[0].Rows[0]["Date"].GetOptionalDate().Value.ToString("d")}" +
                        $"\n\n{queryResponse1.Result.ResultSets[0].Rows[0]["Name"].GetOptionalUtf8()}" +
                        $"\n{queryResponse1.Result.ResultSets[0].Rows[0]["Description"].GetOptionalUtf8()}" +
                        $"\n\n{NewsTextCommands}", $"{queryResponse1.Result.ResultSets[0].Rows[0]["ID"].GetUint64()}.png", 
                        $"{queryResponse1.Result.ResultSets[0].Rows[0]["ImageUrl"].GetOptionalUtf8()}", sender);
                }
                else
                {
                    await SendMessageRequest($"{queryResponse1.Result.ResultSets[0].Rows[0]["Date"].GetOptionalDate().Value.ToString("d")}" +
                        $"\n\n{queryResponse1.Result.ResultSets[0].Rows[0]["Name"].GetOptionalUtf8()}" +
                        $"\n{queryResponse1.Result.ResultSets[0].Rows[0]["Description"].GetOptionalUtf8()}" +
                        $"\n\n{NewsTextCommands}", sender);
                }

                Profile[sender].LastDate = queryResponse1.Result.ResultSets[0].Rows[0]["Date"].GetOptionalDate();
                Profile[sender].LastDate = Profile[sender].LastDate.Value.AddDays(-1);
            }



            else
            {
                await SendMessageRequest($"🚩 Пока что новостей больше нет", sender);
            }


        }
                //Закрыли соединение
         
        private async static void GetOdersHistory(string sender)
        {
            //Получаем сырые данные сетов(без оболочки)
            using (SqlConnection connection = new SqlConnection(PathConnection))
            {
                //Открываем соединение
                try
                {
                    await connection.OpenAsync();
                }
                catch { }

                SqlCommand command1 = new SqlCommand($"SELECT * FROM Orders WHERE AccountPhone = {GetValidPhone(sender)}", connection);

                //Спец список для временного хранения полученных сетов, чтобы потом понять, какой из них переслали
                if (Profile[sender].HistoryOders.Count > 0)
                {
                    Profile[sender].HistoryOders.Clear();
                }

                using (SqlDataReader reader = await command1.ExecuteReaderAsync())
                {
                    if (reader.HasRows) // если есть данные
                    {
                        Profile[sender].typeCommand = TypeCommand.HistoryOders;
                        Profile[sender].SendItemMenuNumber = 0;

                        while (await reader.ReadAsync()) // построчно считываем данные
                        {
                            Profile[sender].HistoryOders.Add(reader.GetValue("Caption").ToString());
                        }

                        for (int i = 0; i < Profile[sender].HistoryOders.Count; i++)
                        {
                            Profile[sender].HistoryOders[i] +=
                           $"{(i == Profile[sender].HistoryOders.Count - 1 ? "" : "\n1 - 💎 Показать ещё")}";
                        }

                        //Отправляем первый сет
                        await SendMessageRequest(Profile[sender].HistoryOders[Profile[sender].SendItemMenuNumber], sender);

                        //Увеличиваем порядковый номер отправленного сета
                        Profile[sender].SendItemMenuNumber++;
                    }
                    else
                    {
                        await SendMessageRequest(DidntOrderYet, sender);
                    }

                    
                }
                //Закрыли соединение
            }
        }

       

    }
}
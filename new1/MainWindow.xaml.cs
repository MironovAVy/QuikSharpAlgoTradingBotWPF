using QuikSharp;
using QuikSharp.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace new1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Quik _quik;
        string secCode = "SBER";
        string classCode = "";
        string clientCode = "";
        private bool isServerConnected = false;
        private Tool tool;
        private bool isSubscribedToolOrderBook = false;
        OrderBook toolOrderBook;
        private MyTimeframe _sber;
        List<MyTimeframe> Timeframe = new List<MyTimeframe>();
        private bool runRobot = true;
        List<Candle> _candleList = new List<Candle>(); 
        public MainWindow()
        {
            InitializeComponent();
            LogTextBox.Text = "";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("Подключаемся к терминалу Quik...");
                _quik = new Quik(Quik.DefaultPort, new InMemoryStorage());    // инициализируем объект Quik
                                                                              //_quik = new Quik(34136, new InMemoryStorage());    // отладочный вариант

            }
            catch
            {
                Log("Ошибка инициализации объекта Quik.");
            }
            if (_quik != null)
            {
                Log("Экземпляр Quik создан.");
                try
                {
                    Log("Получаем статус соединения с сервером...");
                    isServerConnected = _quik.Service.IsConnected().Result;
                    if (isServerConnected)
                    {
                        Log("Соединение с сервером установлено.");
                        Connect.Content = "Ok";
                        Connect.Background = Brushes.Aqua;
                    }
                    else
                    {
                        Log("Соединение с сервером НЕ установлено.");
                    }
                }
                catch
                {
                    Log("Неудачная попытка получить статус соединения с сервером.");
                }

            }
        }

        public void Log(string str)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    LogTextBox.AppendText(str + Environment.NewLine);
                    LogTextBox.ScrollToLine(LogTextBox.LineCount - 1);
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            Run();
        }
        void Run()
        {
            Log("Определяем код класса инструмента " + secCode + ", по списку классов" + "...");
            try
            {
                
                classCode = _quik.Class.GetSecurityClass("SPBFUT,TQBR,TQBS,TQNL,TQLV,TQNE,TQOB,QJSIM,SPBOPT", secCode).Result;
            }
            catch
            {
                Log("Ошибка определения класса инструмента. Убедитесь, что тикер указан правильно");
            }
            if (classCode != null && classCode != "")
            {
                Log("Создаем экземпляр инструмента " + secCode + "|" + classCode + "...");
                tool = new Tool(_quik, secCode, classCode);
                if (tool != null && tool.Name != null && tool.Name != "")
                {
                    Log("Инструмент " + tool.Name + " создан.");
                    Log("Подписываемся на стакан котировок по бумаге " + tool.Name);
                    _quik.OrderBook.Subscribe(tool.ClassCode, tool.SecurityCode).Wait();
                    isSubscribedToolOrderBook = _quik.OrderBook.IsSubscribed(tool.ClassCode, tool.SecurityCode).Result;
                    if (isSubscribedToolOrderBook)
                    {
                        toolOrderBook = new OrderBook();
                        Log("Подписка на стакан прошла успешно.");
                        Log("Подписываемся на колбэк 'OnQuote'...");
                       // _quik.Events.OnQuote += Events_OnQuote;

                    }
                    else
                    {
                        Log("Подписка на стакан не удалась.");

                    }
                    Log("Подписываемся на колбэк 'OnFuturesClientHolding'...");

                    Log("Подписываемся на колбэк 'OnDepoLimit'...");
                    _quik.Candles.NewCandle += Candles_NewCandle; ;
                    Log("Получаем свечи по инструменту '...");
                    _quik.Candles.Subscribe(classCode, secCode, CandleInterval.M1);
                    Log("Получаем свечи по инструменту за последние 10 дней'...");


                    _quik.Events.OnParam += Events_OnParam;

                    _quik.Events.OnOrder += Events_OnOrder;
                    //_quik.Events.OnAllTrade += Events_OnAllTrade;
                    //Log("Создаем опционную доску'...");
                    //var zzz =  _quik.Trading.GetOptionBoard(classCode, secCode).CreationOptions;
                    //var zzz = _quik.Trading.GetOptionBoard(classCode = "SPBOPT", secCode = "SiM4").Result;
                    //Log(zzz.ToString());

                  // foreach (var option in zzz) { Log("Создаем таблицу опционов для  инструмента " + option.OPTIONBASE + "|" + option.DAYSTOMATDATE + "..."); }


                                    }
            }
            else
            {
                Log("Не удалось создать экземпляр инструмента " + secCode + "|" + classCode + "...");
            }
        }

       

        private void Events_OnParam(Param par)
        {
            var OI = _quik.Trading.GetParamEx(classCode, secCode, "NUMCONTRATS").Result.ParamImage;
            var Kol_pok = _quik.Trading.GetParamEx(classCode, secCode, "NUMBIDS").Result.ParamImage;
            var Kol_pro = _quik.Trading.GetParamEx(classCode, secCode, "NUMOFFERS").Result.ParamImage;
            string str = String.Format("{0} {1} {2}", OI, Kol_pok, Kol_pro);
            Log(OI + "  " + Kol_pok + "  " + Kol_pro + "  ");
        }

        

         private void Events_OnAllTrade(AllTrade allTrade)
       {
           //throw new NotImplementedException();
       }

       private void Events_OnOrder(QuikSharp.DataStructures.Transaction.Order order)
       {
           //Log("OrdeNum= " + order.OrderNum +"TransId= " + order.TransID );
           if (order.TransID > 0 ) 
           {
               SellStopLimit(order.Price -10*tool.Step);
           }
       }

       private void Events_OnQuote(OrderBook orderbook)
       {
           if(orderbook.sec_code == secCode)
           {
               var bestBit = orderbook.bid[orderbook.bid.Length-1];
               var bestAsk = orderbook.offer[0];
               string output = String.Format("{0} ------ {1} ---------- {2}--------- {3}", bestBit.price, bestBit.quantity, bestAsk.price,  bestAsk.quantity);
               //Log(output);
               //EnterLong((decimal)bestBit.price + (1 * tool.Step);


           }
       }

       private void Candles_NewCandle(Candle candle)
       {

           //Log(candle.ToString());
           if(runRobot)
           {
               //запуск робота
              // EnterLong(candle.Close);


           }
       }



     private async void RunRobot_Click(object sender, RoutedEventArgs e)
     {

         var option = await _quik.Trading.GetOptionBoard(classCode, secCode);


         foreach (var data in option)
         {
             var dT = data.OPTIONTYPE;
             var S = data.Volatility;
             var sa = data.Strike;

             Log(dT.ToString());
             Log(S.ToString());
         }



     }

        private void ButtonBuy_Click(object sender, RoutedEventArgs e)
        {
            var price = Math.Round(tool.LastPrice + 50 * tool.Step, tool.PriceAccuracy);
            EnterLong(price);
        }
        private void EnterLong(decimal priceIn)
        {
            _quik.Orders.SendLimitOrder(classCode, secCode, tool.AccountID, Operation.Buy, priceIn,1);
        }

        private void EnterShort(decimal priceIn)
        {
            _quik.Orders.SendLimitOrder(classCode, secCode, tool.AccountID, Operation.Sell, priceIn, 1);
        }




        private void SellStopLimit(decimal price)
        {
            decimal priceIn = Math.Round(price, tool.PriceAccuracy);
            StopOrder orderNew = new StopOrder()
            {
                Account = tool.AccountID,
                ClassCode = tool.ClassCode,
                ClientCode = clientCode,
                Quantity = 1,
                StopOrderType = StopOrderType.StopLimit,
                SecCode = secCode,
                Condition = QuikSharp.DataStructures.Condition.LessOrEqual,
                ConditionPrice = priceIn,
                Price = priceIn -50*tool.Step,
                Operation = Operation.Sell,
            };
            _quik.StopOrders.CreateStopOrder(orderNew);
        }

        private void Volatilyty_Click(object sender, RoutedEventArgs e)
        {

            
            

        }
        
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

       

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            _sber = new MyTimeframe();
            Timeframe.Add(_sber);
            DataFrame.ItemsSource = Timeframe;
            DataFrame.Items.Refresh();
        }
    }

}


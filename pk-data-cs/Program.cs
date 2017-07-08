using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pk_data_cs
{
    class Program
    {
        static void Main(string[] args)
        {
            var store = new Store();
            var sideEffects = new SideEffects();
            sideEffects.ApplySideEffects(store);
            Console.Read();
        }
    }

    class Order
    {
        public DateTime CreateTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime PromiseTime { get; set; }
        public string Name { get; set; }

        public int SomeDistanceUnitFrom_1_100_PeekingOrderOnlyIfDistanceAreLessThan_33 { get; set; }

        public override string ToString()
        {

            return string.Format("{0}, TimeLeft:{1}", Name, (PromiseTime - FakeTime.Instance.Now).TotalMinutes);
        }
    }
    class Driver
    {
        public string Name { get; set; }
        public event Action<Driver, Order> OrderArrival;
        public event Action<Driver> DriverArrivalInStore;
        public void AddOrders(Queue<Order> order)
        {

            Task.Run(async () =>
            {
                //a driver need ~5 to peek orders from store
                await FakeTime.Instance.Wait(5);
                for (; order.Count > 0;)
                {
                    //a driver need ~8 min for raod
                    await FakeTime.Instance.Wait(8);
                    //and ~2min for the customer
                    await FakeTime.Instance.Wait(2);
                    if (OrderArrival != null)
                        OrderArrival(this, order.Dequeue());

                }
                //and ~8 min to return
                await FakeTime.Instance.Wait(8);
                DriverArrivalInStore(this);
            });
        }

      

        public void ShiftStart()
        {
            if (DriverArrivalInStore != null)
                DriverArrivalInStore(this);
        }
    }
    class PizzaMaker
    {
        Queue<Order> ordersToMake;
        FakeTime fakeTime;

        public TimeSpan TimeToMakeAPizza { get; set; }
        public int WorkLoad { get { return ordersToMake.Count; } }
        public string Name { get; set; }

        public event Action<PizzaMaker, Order> OrderReady;
        public PizzaMaker()
        {
            ordersToMake = new Queue<Order>();
            TimeToMakeAPizza = TimeSpan.FromMinutes(3.0);
            fakeTime = FakeTime.Instance;
            Task.Run(async () =>
            {
                for (;;)
                {
                    if (ordersToMake.Count > 0)
                    {
                        //i have to make that order
                        var order = ordersToMake.Dequeue();
                        await fakeTime.Wait((int)TimeToMakeAPizza.TotalMinutes);
                        if (OrderReady != null)
                            OrderReady.Invoke(this, order);

                    }

                    await Task.Delay(50);
                }
            });
        }
        public void MakeThatOrderPlz(Order order)
        {
            ordersToMake.Enqueue(order);
        }
    }
    class Foyrnos
    {
        public event Action<Foyrnos, Order> OrderReady;
        public void AddOrder(Order order)
        { 
            FakeTime.Instance.Wait(7, () =>
             {
                 if (OrderReady != null)
                     OrderReady(this, order);
             });
        }
    }

    class Store
    {
        //Store pipeline arrival -> make -> delivery -> end

        List<PizzaMaker> pizzaMakers;
        List<Driver> drivers;
        List<Order> readyOrders;
        Foyrnos foyrnos;

        public TimeSpan PromiseTime { get; set; }


        public Store()
        {
            PromiseTime = TimeSpan.FromMinutes(30);
            foyrnos = new Foyrnos();
            readyOrders = new List<Order>();
            drivers = new List<Driver>();
            pizzaMakers = new List<PizzaMaker>();
            foyrnos.OrderReady += Foyrnos_OrderReady;
        }

    

        public void AddOrder(Order order)
        {
            Console.WriteLine("[{0}] New order {1} arrival", FakeTime.Instance.Now, order.Name);
            order.PromiseTime = order.CreateTime + PromiseTime;
            pizzaMakers
                .OrderBy(x => x.WorkLoad)
                .First()
                .MakeThatOrderPlz(order);
          

        }
        public void AddPizzaMaker(PizzaMaker pizzaMaker)
        {
            Console.WriteLine("[{0}] PizzaMaker (name:{1}) shift started", FakeTime.Instance.Now, pizzaMaker.Name);
            pizzaMaker.OrderReady += PizzaMaker_OrderReady;
            pizzaMakers.Add(pizzaMaker);
        }

        public void AddDriver(Driver driver)
        {
            Console.WriteLine("[{0}] Driver (name:{1}) shift started", FakeTime.Instance.Now, driver.Name);
            driver.OrderArrival += Driver_OrderArrival;
            driver.DriverArrivalInStore += Driver_DriverArrivalInStore;
            driver.ShiftStart();
        }

    
        public void Open()
        {
            Console.WriteLine("[{0}] Store opened", FakeTime.Instance.Now);
        }

        private void PizzaMaker_OrderReady(PizzaMaker arg1, Order arg2)
        {
            //ok put in the "foyrno"
            foyrnos.AddOrder(arg2);
            Console.WriteLine("[{0}] PizzaMaker ({1}) done with order:{2}", FakeTime.Instance.Now, arg1.Name, arg2);
        }
        private void Foyrnos_OrderReady(Foyrnos arg1, Order arg2)
        {
            Console.WriteLine("[{0}] Order:{1} ready for delivery", FakeTime.Instance.Now, arg2);
            readyOrders.Add(arg2);
        }
        private void Driver_OrderArrival(Driver arg1, Order arg2)
        {
            Console.WriteLine("[{0}] Order:{1} delivered by {2}", FakeTime.Instance.Now, arg2, arg1.Name);
        }
        private async void Driver_DriverArrivalInStore(Driver obj)
        {
            //ok we have some driver
            //now just wait for some orders
            while (readyOrders.Count == 0)
                await Task.Delay(50);

            lock (readyOrders)
            {
                
                var peekingFirstOrderForSure = readyOrders.First();
                readyOrders.Remove(peekingFirstOrderForSure);
                var andSomeOrdersThatFit = readyOrders
                    .OrderBy(x =>
                        Math.Abs(peekingFirstOrderForSure.SomeDistanceUnitFrom_1_100_PeekingOrderOnlyIfDistanceAreLessThan_33 - x.SomeDistanceUnitFrom_1_100_PeekingOrderOnlyIfDistanceAreLessThan_33)
                        )
                    .Take(3)
                    .ToList();

                foreach (var item in andSomeOrdersThatFit)
                {
                    readyOrders.Remove(item);
                }
                andSomeOrdersThatFit.Add(peekingFirstOrderForSure);
                obj.AddOrders(new Queue<Order>(andSomeOrdersThatFit));

            }
        }




    }


    class SideEffects
    {
        public async Task ApplySideEffects(Store store)
        {
            var faketime = FakeTime.Instance;
            var rnd = new Random();
            store.Open();
            store.AddPizzaMaker(new PizzaMaker { Name = "billy" });

            await faketime.Wait(10);
            store.AddOrder(new Order { CreateTime = faketime.Now, Name = "pelatis1", SomeDistanceUnitFrom_1_100_PeekingOrderOnlyIfDistanceAreLessThan_33 = rnd.Next(1, 100) });

            await faketime.Wait(15);
            store.AddOrder(new Order { CreateTime = faketime.Now, Name = "pelatis2", SomeDistanceUnitFrom_1_100_PeekingOrderOnlyIfDistanceAreLessThan_33 = rnd.Next(1, 100) });

            await faketime.Wait(15);
            store.AddDriver(new Driver { Name = "eugene" });
            //store.AddDriver(new Driver { Name = "stelios" });
            for (int i = 4; i < 13; i++)
            {
                await faketime.Wait(rnd.Next(4, 10));
                store.AddOrder(new Order { CreateTime = faketime.Now, Name = "pelatis" + i.ToString(), SomeDistanceUnitFrom_1_100_PeekingOrderOnlyIfDistanceAreLessThan_33 = rnd.Next(1, 100) });
            }



        }
    }
    class FakeTime
    {
        public DateTime Now { get; set; }
        static FakeTime instance;
        public static FakeTime Instance
        {
            get
            {
                if (instance == null)
                    instance = new FakeTime();
                return instance;
            }
        }

        public FakeTime()
        {
            //store opening time
            Now = new DateTime(2017, 7, 7, 13, 0, 0);

            Task.Run(async () =>
           {
                //100ms = 1min
                for (;;)
               {
                   Now = Now.AddMinutes(2.0);
                   await Task.Delay(200);
               }
           });
        }
        public async Task Wait(int minInRealTime)
        {
            await Task.Delay(minInRealTime * 100);
        }
        public async Task Wait(int minInRealTime, Action excecuteMePlz)
        {
            await Wait(minInRealTime);
            excecuteMePlz.Invoke();
        }
    }

}

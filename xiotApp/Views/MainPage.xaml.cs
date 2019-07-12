using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using xiotApp.Models;

namespace xiotApp.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage
    {
        Dictionary<int, NavigationPage> MenuPages = new Dictionary<int, NavigationPage>();
        private IMqttClient _mqttClient;

        public MainPage()
        {
            InitializeComponent();

            MasterBehavior = MasterBehavior.Popover;

            MenuPages.Add((int)MenuItemType.Browse, (NavigationPage)Detail);

            MQTTSetup();

            MessagingCenter.Subscribe<ItemsPage, Item>(this, "selected", async (obj, item) =>
            {
                var newItem = item as Item;
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("my/item")
                    .WithPayload(item.Text)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();
                await _mqttClient.PublishAsync(message);
            });
        }

        private async void MQTTSetup()
        {
            // Create a new MQTT client.
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            // Create TCP based options using the builder.
            var options = new MqttClientOptionsBuilder()
                .WithClientId("Client1")
                .WithTcpServer("181.13.53.19", 1771)
                //.WithCredentials("bud", "%spencer%")
                //.WithTls()
                .WithCleanSession()
                .Build();

            _mqttClient.UseDisconnectedHandler(async e =>
            {
                Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await _mqttClient.ConnectAsync(options);
                }
                catch
                {
                    Console.WriteLine("### RECONNECTING FAILED ###");
                }
            });

            _mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                //Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                //Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                //Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                //Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                //Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                //Console.WriteLine();

                //Task.Run(() => _mqttClient.PublishAsync("hello/world"));
                //await Navigation.PushModalAsync(new NavigationPage(new NewItemPage()));
                MessagingCenter.Send(this, "AddItem", new Item() { Id="new", Description= e.ApplicationMessage.Topic, Text= Encoding.UTF8.GetString(e.ApplicationMessage.Payload) });

            });

            _mqttClient.UseConnectedHandler(async e =>
            {
                Console.WriteLine("### CONNECTED WITH SERVER ###");

                // Subscribe to a topic
                await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("my/topic").Build());

                Console.WriteLine("### SUBSCRIBED ###");
            });

            await _mqttClient.ConnectAsync(options);
        }

        public async Task NavigateFromMenu(int id)
        {
            if (!MenuPages.ContainsKey(id))
            {
                switch (id)
                {
                    case (int)MenuItemType.Browse:
                        MenuPages.Add(id, new NavigationPage(new ItemsPage()));
                        break;
                    case (int)MenuItemType.About:
                        MenuPages.Add(id, new NavigationPage(new AboutPage()));
                        break;
                }
            }

            var newPage = MenuPages[id];

            if (newPage != null && Detail != newPage)
            {
                Detail = newPage;

                if (Device.RuntimePlatform == Device.Android)
                    await Task.Delay(100);

                IsPresented = false;
            }
        }
    }
}
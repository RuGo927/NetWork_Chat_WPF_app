using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PublicChatApp
{
    public partial class MainWindow : Window
    {
        static int em = 1; //saniyenin nisbeti (animation emeliyyatlari ucun)

        static string UserName = Environment.MachineName; // default olaraq isdifadeci adi (komputer adi)

        bool isDarkMode = true; // default olaraq gece rejimi

        // Server ve Client ucun deyisenler
        private Socket _listener;               // Server socket
        private List<Socket> _clients = new();  // Qoşulmuş clientlər
        private Socket _clien = null;           // Client socket

        public MainWindow()
        {
            InitializeComponent();
            MessageTextBox.Focus();

            DarkModOffOn(isDarkMode);

        }

        //Menyu ac/bagla buttonuna clik olunduda
        private async void MenyuOnOff(object sender, RoutedEventArgs e)
        {
            if (test.Width.Value == 30)
            {
                while (test.Width.Value < 150)
                {
                    await Task.Delay(10 * em); // daha smooth olsun
                    double newWidth = test.Width.Value + 10;
                    if (newWidth > 150) newWidth = 150;
                    test.Width = new GridLength(newWidth);
                    OnOffBtn.Content = "X";
                }
                return;
            }
            else if (test.Width.Value == 150)
            {
                while (test.Width.Value > 30)
                {
                    await Task.Delay(10 * em);
                    double newWidth = test.Width.Value - 10;
                    if (newWidth <= 30) newWidth = 30;
                    test.Width = new GridLength(newWidth);
                    OnOffBtn.Content = "☰";
                }
                return;
            }
        }

        //Gonderme Buttonuna clik olunduda
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                ShowMessage("Mesaj boş ola bilməz!", "Xəta");
                OkButton.Focus();
                return;
            }
            AddMessage(MessageTextBox.Text, UserName, true);
            MessageTextBox.Focus();
        }

        //UI ye messaji elave edir diyer isdifadecilere gonderir
        private void AddMessage(string message, string name, bool Station = false)
        {

            // Bubble yarat
            Border container = new Border
            {
                Background = Station ? Brushes.LightBlue : Brushes.LightGray,
                CornerRadius = new CornerRadius(0, 8, 8, 8),
                Padding = new Thickness(10),
                Margin = new Thickness(5),

                HorizontalAlignment = Station ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 250
            };

            //yazan isdifadecinin adini yaz
            TextBlock userNameText = new TextBlock
            {
                Text = Station ? $"Siz: ({name})" : name,
                FontWeight = FontWeights.Bold,
                Foreground = Station ? Brushes.White : Brushes.Black
            };

            if (!Station)
                message = message;
            else
                message = MessageTextBox.Text;
            // Mesaj mətnini yaz
            TextBlock messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Station ? Brushes.White : Brushes.Black
            };

            // Saat
            TextBlock timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = Station ? Brushes.White : Brushes.DarkGray
            };

            // İkisini birləşdir (mesaj + saat)
            StackPanel panel = new StackPanel();
            panel.Children.Add(messageText);
            panel.Children.Add(timeText);
            panel.Children.Insert(0, userNameText); // Adı yuxarı əlavə et

            container.Child = panel;


            // ListBox-a əlavə et
            MessagesListBox.Items.Add(container);
            MessagesListBox.ScrollIntoView(container);

            if (_clien != null && Station)
                _clien.Send(Encoding.UTF8.GetBytes($"{name}:{message}"));
            else if (Station)
                BroadcastMessage($"{name}:{message}");
            // TextBox-u təmizlə
            MessageTextBox.Clear();
        }

        //MessageBox OK button closses message box
        private void OkMsgCanvas(object sender, RoutedEventArgs e)
        {
            MsgCanvas.Visibility = Visibility.Hidden;
        }

        //Meessage box usdde cixan zaman animasiya ile gelsin
        public void ShowMessage(string text, string title = "Info")
        {
            MsgText.Text = text;
            MsgTitle.Content = title;
            MsgCanvas.Visibility = Visibility.Visible;
            MsgCanvas.Opacity = 0;

            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,          // başlanğıc şəffaflıq
                To = 1,            // son şəffaflıq
                Duration = TimeSpan.FromMilliseconds(300), // müddət
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // animasiyanı başlad
            MsgCanvas.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            OkButton.Focus();
        }

        //Qrup yarat buttonuna clik olunduda
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            MenyuOnOff(null, null);
            Window CreateGroupWindow = new Window
            {
                Title = "Qrup yarat",
                Width = 350,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(30, 30, 30)) : Brushes.White
            };

            string fg = isDarkMode ? "White" : "Black";

            // Grid yarat
            Grid grid = new Grid { Margin = new Thickness(10) };

            // Row ve Column
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Qrup adı
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Qrup adı TextBox
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Port
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Port TextBox
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Buttons

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            // Qrup adı Label
            Label label = new Label { Content = "Qrup adı:", Foreground = (Brush)new BrushConverter().ConvertFromString(fg) };
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            // Qrup adı TextBox
            TextBox groupNameTextBox = new TextBox { Margin = new Thickness(5) };
            Grid.SetRow(groupNameTextBox, 0);
            Grid.SetColumn(groupNameTextBox, 1);
            grid.Children.Add(groupNameTextBox);

            // Port Label
            Label portLabel = new Label { Content = "Port kodu:", Foreground = (Brush)new BrushConverter().ConvertFromString(fg) };
            Grid.SetRow(portLabel, 1);
            Grid.SetColumn(portLabel, 0);
            grid.Children.Add(portLabel);

            // Port TextBox
            TextBox portTextBox = new TextBox { Margin = new Thickness(5) };
            Grid.SetRow(portTextBox, 1);
            Grid.SetColumn(portTextBox, 1);
            grid.Children.Add(portTextBox);

            // Buttonlar üçün StackPanel
            StackPanel buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(buttonPanel, 2);
            Grid.SetColumnSpan(buttonPanel, 2);

            Button cancelButton = new Button { Content = "Ləğv et", Width = 80, Margin = new Thickness(5, 0, 5, 0) };
            cancelButton.Click += (s, ev) => CreateGroupWindow.Close();

            Button connectButton = new Button { Content = "Bağlan", Width = 80, Margin = new Thickness(5, 0, 5, 0) };
            connectButton.Click += (s, ev) =>
            {
                string name = groupNameTextBox.Text;
                int port = portTextBox.Text != "" && int.TryParse(portTextBox.Text, out int p) ? p : 5000;
                StartServer(port, name);
                CreateGroupWindow.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(connectButton);

            grid.Children.Add(buttonPanel);

            CreateGroupWindow.Content = grid;
            CreateGroupWindow.ShowDialog();

            groupNameTextBox.Focus();
            MessageTextBox.Focus();
        }

        //Port ile baglan buttonuna clik olunduda
        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            MenyuOnOff(null, null);
            Window ConnectWindow = new Window
            {
                Title = "Port ilə Bağlan",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(30, 30, 30)) : Brushes.White
            };

            string fg = isDarkMode ? "White" : "Black";

            // Grid yarat
            Grid grid = new Grid { Margin = new Thickness(10) };

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Port Label
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Port TextBox
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Button Panel

            // Port Label
            Label portLabel = new Label { Content = "Port kodu:", Foreground = (Brush)new BrushConverter().ConvertFromString(fg) };
            Grid.SetRow(portLabel, 0);
            grid.Children.Add(portLabel);

            // Port TextBox
            TextBox portTextBox = new TextBox { Margin = new Thickness(0, 5, 0, 10) };
            Grid.SetRow(portTextBox, 1);
            grid.Children.Add(portTextBox);

            // Buttonlar
            StackPanel buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(buttonPanel, 2);

            Button cancelButton = new Button { Content = "Ləğv et", Width = 80, Margin = new Thickness(5, 0, 5, 0) };
            cancelButton.Click += (s, ev) => ConnectWindow.Close();

            Button connectButton = new Button { Content = "Bağlan", Width = 80, Margin = new Thickness(5, 0, 5, 0) };
            connectButton.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(portTextBox.Text))
                {
                    ShowMessage("Port kodu boş ola bilməz!", "Xəta");
                    portTextBox.Focus();
                    return;
                }



                ConnetServer(portTextBox.Text.Split(":")[0], int.Parse(portTextBox.Text.Split(":")[1]));

                ConnectWindow.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(connectButton);
            grid.Children.Add(buttonPanel);

            ConnectWindow.Content = grid;
            ConnectWindow.ShowDialog();

            portTextBox.Focus();
        }

        //Ayarlar buttonuna clik olunduda
        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            Window settingsWindow = new Window();
            settingsWindow.Title = "Ayarlar";
            settingsWindow.Width = 400;
            settingsWindow.Height = 350;
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settingsWindow.Owner = this;
            settingsWindow.ResizeMode = ResizeMode.NoResize;
            string fg = "Black";
            if (isDarkMode)
            {
                settingsWindow.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                fg = "White";
            }
            else
            {
                settingsWindow.Background = Brushes.White;
                fg = "Black";
            }





            // Ana StackPanel (alt-alta düzülməsi üçün)
            StackPanel mainPanel = new StackPanel
            {
                Margin = new Thickness(10),
                Orientation = Orientation.Vertical
            };

            ScrollViewer scrollViewer = new ScrollViewer
            {
                Content = mainPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // -------------------
            // Edit user name bölməsi
            // -------------------
            #region UserName
            Grid NameGrid = new Grid();
            NameGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            NameGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            NameGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Label nameLabel = new Label { Content = "İstifadəçi adı:" };
            nameLabel.Foreground = (Brush)new BrushConverter().ConvertFromString(fg);

            TextBox nameTextBox = new TextBox { Text = UserName, Margin = new Thickness(0, 5, 0, 10) };
            Button saveButton = new Button { Content = "Yadda saxla", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };

            saveButton.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    ShowMessage("İstifadəçi adı boş ola bilməz!", "Xəta");
                    nameTextBox.Focus();
                    return;
                }
                UserName = nameTextBox.Text;
                ShowMessage($"İstifadəçi adı @{UserName} olaraq dəyişdirildi!", "Info");
            };

            NameGrid.Children.Add(nameLabel);
            NameGrid.Children.Add(nameTextBox);
            NameGrid.Children.Add(saveButton);
            Grid.SetRow(nameLabel, 0);
            Grid.SetRow(nameTextBox, 1);
            Grid.SetRow(saveButton, 2);
            #endregion

            // -------------------
            // Animation setting bölməsi
            // -------------------
            #region Animation
            Grid AnimationSettingGrid = new Grid();
            AnimationSettingGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AnimationSettingGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AnimationSettingGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Label animationLabel = new Label { Content = "Animasiya sürəti (0-10):" };
            animationLabel.Foreground = (Brush)new BrushConverter().ConvertFromString(fg);
            TextBox AnimationNum = new TextBox { Text = em.ToString(), Margin = new Thickness(0, 5, 0, 10) };
            Button AnimationSaveButton = new Button { Content = "Yadda saxla", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };

            AnimationSaveButton.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(AnimationNum.Text) || !int.TryParse(AnimationNum.Text, out int newEm) || newEm < 0 || newEm > 10)
                {
                    ShowMessage("Animasiya sürəti 0-dan 10-a qədər olan rəqəm olmalıdır!", "Xəta");
                    AnimationNum.Focus();
                    return;
                }
                em = newEm;
                ShowMessage($"Animasiya sürəti {em} olaraq dəyişdirildi!", "Info");
            };

            AnimationSettingGrid.Children.Add(animationLabel);
            AnimationSettingGrid.Children.Add(AnimationNum);
            AnimationSettingGrid.Children.Add(AnimationSaveButton);
            Grid.SetRow(animationLabel, 0);
            Grid.SetRow(AnimationNum, 1);
            Grid.SetRow(AnimationSaveButton, 2);
            #endregion


            #region // Gece ve gun rejimi bölməsi (isteğe bağlı, əlavə etdim)
            Grid ModRadioBtn = new Grid();
            ModRadioBtn.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ModRadioBtn.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ModRadioBtn.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Label ModLabel = new Label { Content = "Gecə/Gün rejimi:" };
            ModLabel.Foreground = (Brush)new BrushConverter().ConvertFromString(fg);
            RadioButton DarkModeBtn = new RadioButton { Content = "Gecə rejimi", Margin = new Thickness(0, 5, 0, 5), IsChecked = isDarkMode };
            DarkModeBtn.Foreground = (Brush)new BrushConverter().ConvertFromString(fg);
            RadioButton LightModeBtn = new RadioButton { Content = "Gün rejimi", Margin = new Thickness(0, 5, 0, 10), IsChecked = !isDarkMode };
            LightModeBtn.Foreground = (Brush)new BrushConverter().ConvertFromString(fg);

            Button ModSaveButton = new Button { Content = "Yadda saxla", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
            ModSaveButton.Click += (s, ev) =>
            {
                DarkModOffOn(DarkModeBtn.IsChecked == true);
                ShowMessage($"Rejim {(isDarkMode ? "Gecə" : "Gün")} olaraq dəyişdirildi!", "Info");
            };
            ModRadioBtn.Children.Add(ModLabel);
            ModRadioBtn.Children.Add(DarkModeBtn);
            ModRadioBtn.Children.Add(LightModeBtn);
            ModRadioBtn.Children.Add(ModSaveButton);
            Grid.SetRow(ModLabel, 0);
            Grid.SetRow(DarkModeBtn, 1);
            Grid.SetRow(LightModeBtn, 2);
            Grid.SetRow(ModSaveButton, 3);
            mainPanel.Children.Add(ModRadioBtn);

            #endregion
            // -------------------
            // Əlavə et mainPanel-ə
            // -------------------
            mainPanel.Children.Add(NameGrid);
            mainPanel.Children.Add(new Separator()); // iki hissəni ayırmaq üçün
            mainPanel.Children.Add(AnimationSettingGrid);

            settingsWindow.Content = scrollViewer;
            settingsWindow.ShowDialog();
            MessageTextBox.Focus();
        }

        //Gecə ve gün rejimi funksiyasi
        private void DarkModOffOn(bool DarkMod)
        {
            //ShowMessage("Bu funksiya hələ hazırlanmayıb!", "Info");

            if (DarkMod)
            {
                // Dark mode üçün rəngləri təyin et
                this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                MessagesListBox.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                MessageTextBox.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                MessageTextBox.Foreground = Brushes.White;
                ChatName.Foreground = Brushes.White;
                TextBoxTitle.Foreground = Brushes.White;
                Menyu.Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                menyuBorder.BorderBrush = Brushes.Gray;
                MenyuTitle.Foreground = Brushes.White;
                MsgBorder.Background = new SolidColorBrush(Color.FromRgb(140, 110, 110));
                isDarkMode = true;
            }
            else
            {
                // Light mode üçün rəngləri təyin et
                this.Background = Brushes.White;
                MessagesListBox.Background = Brushes.White;
                MessageTextBox.Background = Brushes.White;
                MessageTextBox.Foreground = Brushes.Black;
                ChatName.Foreground = Brushes.Black;
                TextBoxTitle.Foreground = Brushes.Black;
                Menyu.Background = Brushes.LightGray;
                MenyuTitle.Foreground = Brushes.Black;
                menyuBorder.BorderBrush = Brushes.DarkGray;
                MsgBorder.Background = new SolidColorBrush(Color.FromRgb(140, 125, 125));
                isDarkMode = false;
            }




        }

        //Serveri işə salır
        private void StartServer(int port, string name)
        {
            Task.Run(() =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        MsgContainer.IsEnabled = true;
                        ChatName.Content = $"{name} : {port}";
                    });
                    _listener = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);

                    var ip = IPAddress.Any;
                    var ep = new IPEndPoint(ip, port);

                    _listener.Bind(ep);
                    _listener.Listen(10);

                    Dispatcher.Invoke(() =>
                        ShowMessage($"Server {ep} ünvanında işə düşdü.", "Info"));

                    while (true)
                    {
                        var client = _listener.Accept();
                        _clients.Add(client);

                        Dispatcher.Invoke(() =>
                        {
                            client.Send(Encoding.UTF8.GetBytes($"D@T@ : {name}/{port}/{UserName}"));
                            ShowMessage($"@{client.RemoteEndPoint} qoşuldu.", "New User..");
                            OnlineUserList();
                        });

                        Task.Run(() => HandleClient(client));
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MsgContainer.IsEnabled = true;
                        ShowMessage($"Server xətası: {ex.Message}", "Error");
                    });
                }
            });
        }

        //Clientden gelen melumatlari qebul edir
        private void HandleClient(Socket client)
        {
            var buffer = new byte[1024];
            try
            {
                while (true)
                {
                    var len = client.Receive(buffer);
                    if (len <= 0) break;

                    var msg = Encoding.UTF8.GetString(buffer, 0, len);


                    Dispatcher.Invoke(() =>
                    {
                        if (msg.StartsWith("D@T@"))
                        {
                            string Sname = msg.Split(":")[1].Split("/")[0];
                            string port = msg.Split(":")[1].Split("/")[1];
                            string Creator = msg.Split(":")[1].Split("/")[2];

                            ChatName.Content = $"{Sname} : {port} - Creator [{Creator}]";
                        }
                        else
                            AddMessage(msg.Split(":")[1], msg.Split(":")[0], false);
                    });
                    foreach (var c in _clients)
                    {
                        if (c != client)
                            c.Send(Encoding.UTF8.GetBytes(msg));
                    }
                }
            }
            catch { }
            finally
            {
                Dispatcher.Invoke(() =>
                    ShowMessage($"{client.RemoteEndPoint} bağlantısı kəsildi.", "Info"));
                _clients.Remove(client);
                MsgContainer.IsEnabled = false;
                client.Close();
                Dispatcher.Invoke(() => OnlineUserList());
            }
        }

        //Bütün clientlere mesaj gonderir
        public void BroadcastMessage(string msg)
        {

            byte[] data = Encoding.UTF8.GetBytes(msg);

            foreach (var c in _clients.ToList())
            {
                try
                {
                    c.Send(data);
                }
                catch
                {
                    _clients.Remove(c);
                    c.Close();
                }
            }
        }

        //Port ile servere qosulur
        private async void ConnetServer(string Id, int port)
        {
            ShowMessage($"Port kodu ilə bağlantı qurulur... ({port})", "Info");

            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clien = client;
            var ipAddress = IPAddress.Parse(Id);
            var serverEndPoint = new IPEndPoint(ipAddress, port);

            try
            {
                // Serverə qoşul
                await Task.Run(() => client.Connect(serverEndPoint));

                if (client.Connected)
                {
                    ShowMessage($"Serverə qoşuldu! ({serverEndPoint})", "Info");
                    MsgContainer.IsEnabled = true;
                    Task.Run(() => HandleClient(client));
                }
                else
                {
                    ShowMessage($"Serverə qoşulma mumkun olmadi ({serverEndPoint})", "Info");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MsgContainer.IsEnabled = false;
                    ShowMessage($"Bağlantı xətası: {ex.Message}", "Error");
                });
            }
        }

        //Online olan istifadecilerin siyahisini gosterir (Update edir)
        private void OnlineUserList()
        {
            onlineListBox.Items.Clear(); // Əvvəlcə siyahını təmizləyək

            foreach (var c in _clients.ToList())
            {
                string clientEndpoint = c.RemoteEndPoint.ToString();

                try
                {
                    // UI thread-də işlətmək üçün Dispatcher istifadə et
                    Dispatcher.Invoke(() =>
                    {
                        onlineListBox.Items.Add(clientEndpoint);
                    });
                }
                catch
                {
                    // Problem yaranarsa clienti sil
                    _clients.Remove(c);
                    c.Close();
                }
            }
        }

    }
}
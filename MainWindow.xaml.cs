﻿/*
* Filename: MainWindow.xaml.cs
* Author:   Korneva Anastasiia
* Date:     23.02.2018
* This file contains all functionality for user side of chat application. It allows to connect to server and send/recive messages for chat purpose and provide UI functionality for user.
* Code base: Windows and Mobile Programing Assignment 4 by Lauria Victor, Korneva Anastasiia(12.12.2017)
* Ecryption key generated by:http://www.andrewscompanies.com/tools/wep.asp
*/



using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int bufLen = 1024;
        const string goodBye = "Bye!";//exit chat message
        const string doEncrypt = "  ";//signal to start encrypting
        const string stopEncrypt = " stop ";
        static bool nameChanged = false;
        static bool messageSelected = false;
        //for looping encrypt and decrypt
        static bool doEncr = false;
        static bool doDecr = false;
        static string name = "";
        bool encryptText = false;

        private static string myPrivateChat = "";

        public static string ChatBuffTest { get { return myPrivateChat; } set { myPrivateChat = value; } }

        public delegate void UpdateTextCallback(string message);

        encrypt encrypt = new encrypt();
        TcpClient client = null;
        public MainWindow()
        {
            InitializeComponent();
            BtnConnect.Focus();
        }



        //Function: private void Connect(object sender, RoutedEventArgs e)
        //Description: This function creates a client connection if possible and if it has user name, if not inform user about problems to connect to server.
        //			  
        //Parameter: object sender
        //           RoutedEventArgs e
        //Return Value: none
        private void Connect(object sender, RoutedEventArgs e)
        {
            if (CheckName()) // check if name exists
            {
                if (client == null)
                {
                    //connection information
                    IPAddress ip = IPAddress.Parse("127.0.0.1");
                    int port = 5000;
                    client = new TcpClient();

                    try
                    {
                        client.Connect(ip, port);
                        Console.WriteLine("client connected!!");
                    }
                    catch(TimeoutException toe)
                    {
                        // unable to connect
                        MessageBox.Show(toe.Message, "Unable to connect", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        // set focus
                    }
                    catch(IOException ioe)
                    {
                        // unable to connect
                        MessageBox.Show(ioe.Message, "Unable to connect", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }

                    // check if it's connected
                    if (client!=null)
                    {
                        // change NameField
                        NameField.IsReadOnly = true;
                        NameField.Background = Brushes.LightGray;

                        // Change button
                        BtnConnect.Content = "Connected!";
                        BtnConnect.IsEnabled = false;

                        // connect to receiver
                        Thread receiver = new Thread(ClientThreadReceiver);
                        receiver.Start();

                        // set focus
                        Message.Focus();
                    }
                    else
                    {
                        client = null;

                        // set focus
                    }
                }
            }
            else
            {
                // name is invalid for some reason
                MessageBox.Show("Please insert a valid user name!", "Invalid user name", MessageBoxButton.OK, MessageBoxImage.Information);
                // set focus
                NameField.Focus();
            }
        }


        
        //Function: private static void ClientThreadReceiver(object data)
        //Description: This function creates a thread to recive information from server and recives this information and interpriet to user.
        //			  
        //Parameter: object data
        //Return Value: none
        private void ClientThreadReceiver(object data)
        {
            NetworkStream input = client.GetStream();
            //beffers to process information
            string chatBuffer = "";
            string output = "";
            string fromServer = "";
            bool done = false;

            string key = "nrtRQ";//key to decrypt
            byte[] asciiKey = System.Text.Encoding.ASCII.GetBytes(key);

            while (!done)
            {
                byte[] receivedBytes = new byte[bufLen];
                int byte_count;
                //get information
                if ((byte_count = input.Read(receivedBytes, 0, receivedBytes.Length)) <= 0)
                {
                    done = true;
                }
                byte[] cb = System.Text.Encoding.ASCII.GetBytes(doEncrypt);
                chatBuffer = (Encoding.ASCII.GetString(receivedBytes, 0, byte_count));
                string temp = chatBuffer;
                string de = (Encoding.ASCII.GetString(cb, 0, doEncrypt.Length));
                chatBuffer = temp.Substring(0, temp.Length - 2);
                //identify if information requer decryption
                if (chatBuffer == goodBye)
                {
                    done = true;
                }

                else if (chatBuffer == de)
                {
                    doDecr = true;
                }
               
                else
                {
                    fromServer = chatBuffer;
                    //decrypt
                    if (doDecr == true)
                    {
                        chatBuffer = chatBuffer + "\0";
                        output = chatBuffer;
                        encrypt.BlowFish(asciiKey);
                        output = encrypt.Decrypt_CBC(output);
                        if (chatBuffer == stopEncrypt)
                        {
                            doDecr = false;
                        }
                        else
                        {
                            chatBuffer = fromServer + '\n' + output;
                        }
                    }
                    else
                    {
                        chatBuffer = fromServer;
                    }
                    ChatHistory.Dispatcher.Invoke
                    (
                        new UpdateTextCallback(this.ReceiveChat),
                        new object[] { chatBuffer.ToString() }
                    );
                }
            }
        }



        //Function: private void ReceiveChat(string message)
        //Description: This function breaks messages to line and disolays in TextBox
        //			  
        //Parameter: string message: messages to be displayed
        //Return Value: none
        private void ReceiveChat(string message)
        {
            ChatBuffTest += message + "\n";
            ChatHistory.Text = ChatBuffTest;
        }



        //Function: private void Send(object sender, RoutedEventArgs e)
        //Description: This function send message to server or exit chat is user have send message to do this
        //			  
        //Parameter: object sender
        //           RoutedEventArgs e
        //Return Value: none
        private void Send(object sender, RoutedEventArgs e)
        {
            NetworkStream input = client.GetStream();

            byte[] buffer;// = Encoding.ASCII.GetBytes(doEncrypt);
            
            if (client != null)
            {

                if (Message.Text == goodBye)
                {
                    Exit(sender, null);
                }
                else
                {
                    if(encryptText == true)
                    {
                        buffer = Encoding.ASCII.GetBytes(doEncrypt);
                        input.Write(buffer, 0, buffer.Length);
                        doEncr = true;
                        encryptText = false;
                    }
                    //format message
                    Message.Text = name + "> " + Message.Text;
                    if (doEncr == true)
                    {
                        Message.Text = encrypt.Encrypt_CBC(Message.Text);
                    }
                    buffer = Encoding.ASCII.GetBytes(Message.Text);
                    input.Write(buffer, 0, buffer.Length);
                    Message.Text = "";
                }
            }
            Message.Focus();
        }



        //Function: private void NameField_KeyDown(object sender, KeyEventArgs e)
        //Description: This function does ChatHistory disabled to use
        //			  
        //Parameter: object sender
        //           KeyEventArgs e
        //Return Value: none
        private void Exit(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                client.Close();
                Console.WriteLine("disconnect from server!!");
            }
            Environment.Exit(0);
        }



        //Function: private void ChatHistory_KeyDown(object sender, KeyEventArgs e)
        //Description: This function does ChatHistory disabled to use
        //			  
        //Parameter: object sender
        //           KeyEventArgs e
        //Return Value: none
        private void ChatHistory_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Send(sender, e);
            }
        }



        //Function: private void NameField_KeyDown(object sender, KeyEventArgs e)
        //Description: This function does NmaeFiled disabled to use
        //			  
        //Parameter: object sender
        //           KeyEventArgs e
        //Return Value: none
        private void NameField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Connect(sender, e);
            }
        }




        //Function: private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        //Description: This function exits chat and close connection if user closes chat window
        //			  
        //Parameter: object sender
        //           System.ComponentModel.CancelEventArgs e
        //Return Value: none
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Exit(sender, null);
        }



        //Function: private void Message_GotFocus(object sender, RoutedEventArgs e)
        //Description: This function makes name box avaluable for user
        //			  
        //Parameter: object sender
        //           RoutedEventArgs e
        //Return Value: none
        private void NameField_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!nameChanged)
            {
                NameField.Text = "";
                nameChanged = true;
            }
        }



        //Function: private void Message_GotFocus(object sender, RoutedEventArgs e)
        //Description: This function makes message box avaluable for user
        //			  
        //Parameter: object sender
        //           RoutedEventArgs e
        //Return Value: none
        private void Message_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!messageSelected)
            {
                Message.Text = "";
                messageSelected = true;
            }
        }



        //Function: private bool CheckName()
        //Description: This function checks if user has enetere his name
        //			  
        //Parameter: none
        //Return Value: retCode: true if user entered name and false if not
        private bool CheckName()
        {
            bool retCode = false;

            if (nameChanged)
            {
                if (NameField.Text != "")
                {
                    name = NameField.Text.ToString();
                    retCode = true;
                }
            }

            return retCode;
        }



        //Function: private void ChatHistory_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        //Description: This function scrolls messge space if nessesary to make all messages easy to read.
        //			  
        //Parameter: object sender
        //           System.Windows.Controls.TextChangedEventArgs e
        //Return Value: none
        private void ChatHistory_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ChatHistory.ScrollToEnd();
        }



        //Function: private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        //Description: This function addops size of TextBox and messages displaying depend on of application size customized by user.
        //			  
        //Parameter: object sender
        //           SizeChangedEventArgs e
        //Return Value: none
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ChatHistory.ScrollToEnd();
        }



        //Function: private void RadioButton_Checked(object sender, SizeChangedEventArgs e)
        //Description: This function hendle if user want to encrypt information.
        //			  
        //Parameter: object sender
        //           SizeChangedEventArgs e
        //Return Value: none
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            encryptText = true;
            encrypt.BlowFish("6e72745251");
        }
    }
}

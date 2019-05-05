//! \file Program.cs
//! \author Niccolò Fasolo
//! \date 03/05/2019
//! \version 1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;
using System.IO.Ports;

namespace RaspApp
{
    //! \class Program
    //! \brief Classe del programma principale
    class Program
    {
		static SerialPort port1 = new SerialPort("COM5", 9600, Parity.None, 8, StopBits.One); // porta seriale a cui è collgato Arduino
        static string sendValue = ""; // variabile condivisa tra reader e sender

        /* variabili di connessione */
        public static ConnectionFactory factory;
        public static IConnection connection;
        public static IModel channel;

        //! \fn Main
        //! \brief Programma principale
        static void Main(string[] args)
        {
            Thread reader = new Thread(new ThreadStart(Reader));
            Thread sender = new Thread(new ThreadStart(Sender));

			//string[] ports = SerialPort.GetPortNames();

            /* apre una connessione con il broker RabbitMQ */
            factory = new ConnectionFactory() { HostName = "nccfsl.changeip.org", UserName = "3AII", Password = "3aiirossi" };
            try
            {
                connection = factory.CreateConnection();
                channel = connection.CreateModel();
                channel.QueueDeclare(queue: "data",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
            }
            catch (Exception)
            {
                /* se la connessione non riesce */
                Console.WriteLine("Unable to connect to the server. Reopen application to retry the connection.");
                Console.ReadKey();
                Environment.Exit(1);
            }

            /* se la connessione riesce */
            Console.WriteLine("Connected to server. Ready to send data.");

            /* avvio le due thread */
            reader.Start();
            sender.Start();
        }

        //! \fn Reader
        //! \brief Funzione della prima thread, legge da porta seriale e salva in variabile condivisa
        static void Reader() // thread reader (legge dalla seriale)
        {
            port1.Open();
            while (true) 
			{
	            string s = port1.ReadExisting();

	            lock (sendValue) { sendValue = s; }

	            Thread.Sleep(5000); // aspetta 2 secondi
			}
        }

        //! \fn Sender
        //! \brief Funzione della seconda thread, legge i dati dad variabile condivisa e li invia al broker RabbitMQ
        static void Sender() // thread sender (invia i dati al broker)
        {
			while (true) 
			{
	            lock (sendValue)
	            {
	                string sendVar = sendValue;
	                var body = Encoding.UTF8.GetBytes(sendVar);

	                /* carica i dati sul broker RabbitMQ */
	                channel.BasicPublish(exchange: "",
	                                     routingKey: "data",
	                                     basicProperties: null,
	                                     body: body);

	                Console.WriteLine("Sent {0}", sendVar);
	            }

	            Thread.Sleep(5000); // aspetta 2 secondi
			}
        }
    }
}

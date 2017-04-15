﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System;


/*The actual structure standard for the messages to be passed to communicate in the application.*/
public struct Message
{
    public char m_cIsForServer;                    //Tipo de usuario (cliente o server)
    public string m_szSenderID;                    //ID de quien lo envía
    public string m_szTargetAddress;               //IP de a quién va dirigido ?
    public string m_szMessageType;                 //tipo de mensaje (iniciar conexión, dejar conexión, update, etc.)
    public string m_szMessageContent;              //Contenido del mensaje

    public Message(byte[] in_receivedBytes)
    {
        string tmpString = Encoding.UTF8.GetString(in_receivedBytes);
        Debug.Log("Constructing a Message with: " + tmpString);
        string[] tmpValuesArray = tmpString.Split('\t'); //Gives us 5 parts so we can use each one as one of the variables of this object.
        if ( tmpValuesArray.Length != 5 )
        {
            //Then, the received bytes did not have the correct format (which is, containing 4 tabs '\t' to addecuately make the split).
            Debug.LogError("A message was constructed without the correct information. ");
            m_cIsForServer = '0';
            m_szSenderID = null;
            m_szTargetAddress = null;
            m_szMessageType = null;
            m_szMessageContent = null;
            return; //return to exit the constructor.
        }

        //Else, everything is formated correctly, so we can create our message object easily.
        m_cIsForServer = tmpValuesArray[0].ToCharArray()[0]; //By convention, this is where we will store this information.
        m_szSenderID = tmpValuesArray[1];
        m_szTargetAddress = tmpValuesArray[2];
        m_szMessageType = tmpValuesArray[3];
        m_szMessageContent = tmpValuesArray[4];

        Debug.Log("A message was created, and it has the values: " + ToString());//Debug to see what does it say.
    }

    public Message(char in_isForServer, string in_szSenderID, string in_szTargetIPAddress, string in_szTypeOfMessage, string in_szMessageContent)
    {
        m_cIsForServer = in_isForServer; //By convention, this is where we will store this information.
        m_szSenderID = in_szSenderID;
        m_szTargetAddress = in_szTargetIPAddress;
        m_szMessageType = in_szTypeOfMessage;
        m_szMessageContent = in_szMessageContent;
    }

    //Overrided method to obtain a string with exactly the format desired to make the functioning better and easier.
    public override string ToString()
    {
        string tmpString;
        tmpString = m_cIsForServer.ToString() + '\t' + m_szSenderID + '\t' + m_szTargetAddress + '\t' + m_szMessageType + '\t' + m_szMessageContent;
        return tmpString;
    }

};

public struct ClientInfo
{
    public int m_iID;
    public String m_szIPAdress;

    public override int GetHashCode()
    {
        return m_szIPAdress.GetHashCode();
    }
    //Need anything else?
};

public class CServer : MonoBehaviour
{
	/*< Cached server. */
	static CServer m_CachedServer;
    //CThreadManager m_ThreadManager;

    public string m_szMulticastIP = "223.0.0.0"; //default INVALID Multicast IP for this program.
    public int m_iMulticastPort = 10000;

	//Socket  m_Socket,
	//		m_SocketTick;

    //Supposedly, this one will no longer be necessary.
    UdpClient m_udpServer;

    public CClient m_pClientRef = null;

	//ArrayList m_lstClients = new ArrayList();
    //Used to store information about the connected clients to this server.
    //List<ClientInfo> m_lstClientInfo = new List<ClientInfo>();
    HashSet<ClientInfo> m_setClientInfo = new HashSet<ClientInfo>();
    public List<Message> m_MessagesList = new List<Message>();

    private int m_iCurrentID = 0;

    private int GetNewID()
    {
        return m_iCurrentID++;
    }

    private int GetHighestID()
    {
        int iHighest = 0;
        foreach( ClientInfo cinfo in m_setClientInfo)
        {
            iHighest = iHighest < cinfo.m_iID ? cinfo.m_iID : iHighest; //If in one line to update the actual iHighest.
        }

        return iHighest;
    }

    public static CServer CachedServer
	{
		get { return m_CachedServer; }
	}

	void Awake( )
	{
		//m_ThreadManager = new CThreadManager();
		m_CachedServer = this;

		//m_Socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
		////m_SocketTick = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
		//IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, 10000);
		//m_Socket.ExclusiveAddressUse = false;
		////m_Socket.Blocking = false;
		//m_Socket.Bind( ipLocal );
		//m_Socket.Listen( 100 );
		////m_SocketTick.Listen( 11000 );
		//Thread newThread = new Thread( openTCP );
		//Thread newThread2 = new Thread( openUDP );
		//newThread.Start();
		//newThread2.Start();
	}

    //Starts this component as the active server.
    public void StartServer( CClient in_pClientRef,  HashSet<ClientInfo> in_refKnownClients)
    {
        Debug.Log("Entered StartServer function.");
        //m_udpServer.Client.ExclusiveAddressUse = false; //as it'll send and recieve.
        //m_udpServer = new UdpClient(/*10000*/); //port 10,000 as a convention

        m_pClientRef = in_pClientRef;//Copy the reference, so it can be later used to use its socket to send and receive.

        if (m_pClientRef.m_szMulticastIP == "223.0.0.0") //if it is equal to thge default Multicast address, then it's a complete new server-
        {
            //We generate a new one, so they don't use a "pre-known" IP address, so we can protect the system a little more.
            int[] iRand = new int[4];
            iRand[0] = UnityEngine.Random.Range(224, 239);
            iRand[1] = UnityEngine.Random.Range(0, 255);
            iRand[2] = UnityEngine.Random.Range(0, 255);
            iRand[3] = UnityEngine.Random.Range(0, 255);
            m_szMulticastIP = iRand[0].ToString() + "." + iRand[1].ToString() + "." + iRand[2].ToString() + "." + iRand[3].ToString(); //new composed address.
            m_iMulticastPort = UnityEngine.Random.Range(10000, 11000);//some range of possible ports.
        }
        else //else, we adopt the previously stablished Address and port of the multicast group.
        {
            m_szMulticastIP = in_pClientRef.m_szMulticastIP;
            m_iMulticastPort = in_pClientRef.m_iMulticastPort;
        }

        Debug.LogWarning("The IP Address and port for the Multicast group are:  " + m_szMulticastIP + " : " + m_iMulticastPort.ToString());

        m_setClientInfo.Clear();//Clear it from any possible trash from other executions o something else.

        m_setClientInfo = new HashSet<ClientInfo>(in_refKnownClients); //Done this way so it copies the elements of that set into its own container.
        m_iCurrentID = GetHighestID(); //The highest (lowest, in this case) ID given in the elements of "m_setClientInfo".


        //m_udpServer.BeginReceive(new AsyncCallback(ServerReceiveCallback), null); //To start receiving asynchronously.
        Debug.Log("Exit of StartServer function.");
    }



    void Update()
    {
        ProcessMessages(); //Just call ProcessMessages for now, if there is anything else, put it here.
    } 

    private void ProcessMessages()
    {
        
        while (m_MessagesList.Count != 0)
        {
            Message pActualMessage = m_MessagesList[0]; //Get the first element opf the container.
            m_MessagesList.RemoveAt(0); //Then, remove it from the container.

            Debug.Log("Processing message with contents: " + pActualMessage.ToString());
            
            //Check which type of message is.
            switch (pActualMessage.m_szMessageType)
            {
                case "Begin_Con": //Which is begin connection.
                    {
                        //he multicast address range is 224.0.0.0 to 239.255.255.255. If you specify an address outside this range or if the router to which 
                        //the request is made is not multicast enabled, UdpClient will throw a SocketException.
                        Debug.Log("Entered Begin_Con case. A new client has requested to begin connection to this server.");
                        ClientInfo tmpInfo = new ClientInfo();
                        tmpInfo.m_iID = int.Parse( pActualMessage.m_szSenderID); //Serves as a casting to int.

                        tmpInfo.m_szIPAdress = pActualMessage.m_szTargetAddress;  //The position of the bytes corresponding to the IP Address.

                        Debug.Log("That client's IP Address is : " + tmpInfo.m_szIPAdress);

                        //If it has not been registered as a connected client, then, add it to the list.
                        if (IsNewIPAddress(tmpInfo))
                        {
                            //This means it is new to this server. Check if it was already registered to another one. Do it by checking its ID.
                            if (tmpInfo.m_iID == 0)
                            {
                                //It means it is a completely new client, not registered before. So have to assign a new ID to it.
                                tmpInfo.m_iID = GetNewID();

                                //Send the IP of the multicast group to the new client, so it can join. Also, its new ID for inside the group.
                                //NOTE:::: CHECK IF IT IS NECESSARY TO PASS THE SERVER IP too.
                                Message MulticastAddressMsg = new Message('N', m_pClientRef.m_szClientIP, pActualMessage.m_szTargetAddress, "Conn_Accepted", (m_szMulticastIP + "\t" + m_iMulticastPort.ToString() + "\t" + tmpInfo.m_iID.ToString()));
                                m_pClientRef.SendUDPMessage(MulticastAddressMsg, IPAddress.Parse(pActualMessage.m_szTargetAddress), 10000); //send it by the default port: 10000

                                //Now, send a message to that user, confirming its connection was successful. 
                                Debug.LogWarning("A new client is being connected. Notifying all other active users about this. Its ID will be: " + tmpInfo.m_iID);
                                m_pClientRef.SendUDPMessage('N', "New_User", tmpInfo.m_iID.ToString() + "\t" + pActualMessage.m_szTargetAddress, IPAddress.Parse(m_szMulticastIP), m_iMulticastPort); //send it to the multicast group.

                            }
                            else // else, it means that the client had an ID assigned by the previous server, but this machine didn't know about it. 
                            {
                                //So, we just add it to the set, without incrementing the Current ID.
                                //First, check if it is a lower ID than the Highest one this server knows.
                                if(tmpInfo.m_iID > m_iCurrentID)
                                    Debug.LogError("A client with an ID higher than the actual known highest ID has arrived, please corroborate this.");

                                Debug.Log("An old Client has been added to the hash.");
                                //In any case, add it.
                            }

                            //Add it to the Set of ClientsInfo.
                            //NOTE:::: Maybe it's not necessary anymore. 15 / april 2017
                            m_setClientInfo.Add(tmpInfo);

                            //SEND TO EVERYONE ON THE GROUP.
                            /*********/
                            //TO DO 
                            Debug.Log("Sending to everyone else the info about the recently connected client.");
                            //PUT THE SEND COMMAND TO THE Multicast Group.

                        }
                        else
                        {
                            Debug.Log("Someone who is already connected tried to connect. Its address is: " + tmpInfo.m_szIPAdress);
                        }
                        Debug.Log("Exit Begin_Con case in the server.");
                    }
                    break;
                default:
                    {
                        Debug.LogError("ERROR: The server received a pActualMessage.m_szMessageType with value: " + pActualMessage.m_szMessageType);
                    }
                    break;


            }

            //Begin connection message.

            //In-game message, such as action performed.

            
        }

    }

    private bool IsNewIPAddress(ClientInfo in_AddressToCheck)
    {
        if (m_setClientInfo.Contains(in_AddressToCheck))
        {
            return false;
        }

        //If no address like that has been registered, return true.
        return true;
    }

	//void openTCP( )
	//{
	//	try
	//	{
	//		Socket tmpSocket = m_Socket.Accept();
	//		if ( tmpSocket != null )
	//		{
	//			string ipAddress = ((IPEndPoint)tmpSocket.RemoteEndPoint).Address.ToString();
	//			Debug.Log( ipAddress );
	//			CClientHandler newClient = new CClientHandler( tmpSocket, ipAddress );
	//			Thread newThread = new Thread( newClient.run );
	//			m_lstClients.Add( newClient );
	//			m_ThreadManager.AddThread( newClient.run );
	//			newThread.Start();
	//		}
	//	}
	//	catch ( SocketException e )
	//	{
	//		//Debug.Log( e );
	//	}
	//}
	//void openUDP( )
	//{
	//}
}

//public class CThreadManager
//{
//	ArrayList m_lstThreads = new ArrayList();
//	public CThreadManager()
//	{
//	}
//	public void AddThread( ThreadStart in_function )
//	{
//		Thread newThread = new Thread( in_function );
//		m_lstThreads.Add( newThread );
//	}
//	public void EndThread( int in_ThreadNumber )
//	{
//		( (Thread)m_lstThreads[in_ThreadNumber] ).Interrupt();//If necessary use abort.
//		m_lstThreads.RemoveAt( in_ThreadNumber );
//	}
//}


//public class CClientHandler
//{
//	Socket m_Socket;
//	string m_strIpAddress;
//	public CClientHandler( Socket in_Socket, string in_strIpAddress )
//	{
//		m_Socket = in_Socket;
//		m_strIpAddress = in_strIpAddress;
//	}
//	public void run( )
//	{
//		while ( true )
//		{
//			Debug.Log( "Update: " + m_strIpAddress );
//		}
//	}
//}

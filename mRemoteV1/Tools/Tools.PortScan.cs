using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Connection.Protocol.Http;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Connection.Protocol.Rlogin;
using mRemoteNG.Connection.Protocol.SSH;
using mRemoteNG.Connection.Protocol.Telnet;
using mRemoteNG.Connection.Protocol.VNC;
using mRemoteNG.Messages;

namespace mRemoteNG.Tools
{
	public class ScanHost
    {
        #region Private Variables

	    #endregion

        #region Properties
        public static int SSHPort { get; set; } = (int)ProtocolSSH1.Defaults.Port;

	    public static int TelnetPort { get; set; } = (int)ProtocolTelnet.Defaults.Port;

	    public static int HTTPPort { get; set; } = (int)ProtocolHTTP.Defaults.Port;

	    public static int HTTPSPort { get; set; } = (int)ProtocolHTTPS.Defaults.Port;

	    public static int RloginPort { get; set; } = (int)ProtocolRlogin.Defaults.Port;

	    public static int RDPPort { get; set; } = (int)ProtocolRDP.Defaults.Port;

	    public static int VNCPort { get; set; } = (int)ProtocolVNC.Defaults.Port;

	    public string HostName { get; set; } = "";

	    public string HostNameWithoutDomain
		{
			get
			{
				if (string.IsNullOrEmpty(HostName) || HostName == HostIp)
				{
					return HostIp;
				}
				return HostName.Split('.')[0];
			}
		}

        public string HostIp { get; set; }

	    public ArrayList OpenPorts { get; set; }

	    public ArrayList ClosedPorts { get; set; }

	    public bool RDP { get; set; }

	    public bool VNC { get; set; }

	    public bool SSH { get; set; }

	    public bool Telnet { get; set; }

	    public bool Rlogin { get; set; }

	    public bool HTTP { get; set; }

	    public bool HTTPS { get; set; }

	    #endregion
				
        #region Methods
		public ScanHost(string host)
		{
			HostIp = host;
			OpenPorts = new ArrayList();
			ClosedPorts = new ArrayList();
		}
				
		public override string ToString()
		{
			try
			{
				return "SSH: " + Convert.ToString(SSH) + " Telnet: " + Convert.ToString(Telnet) + " HTTP: " + Convert.ToString(HTTP) + " HTTPS: " + Convert.ToString(HTTPS) + " Rlogin: " + Convert.ToString(Rlogin) + " RDP: " + Convert.ToString(RDP) + " VNC: " + Convert.ToString(VNC);
			}
			catch (Exception)
			{
				Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, "ToString failed (Tools.PortScan)", true);
				return "";
			}
		}
				
		public ListViewItem ToListViewItem()
		{
			try
			{
			    ListViewItem listViewItem = new ListViewItem
			    {
			        Tag = this,
			        Text = !string.IsNullOrEmpty(HostName) ? HostName : HostIp
			    };

			    listViewItem.SubItems.Add(BoolToYesNo(SSH));
				listViewItem.SubItems.Add(BoolToYesNo(Telnet));
				listViewItem.SubItems.Add(BoolToYesNo(HTTP));
				listViewItem.SubItems.Add(BoolToYesNo(HTTPS));
				listViewItem.SubItems.Add(BoolToYesNo(Rlogin));
				listViewItem.SubItems.Add(BoolToYesNo(RDP));
				listViewItem.SubItems.Add(BoolToYesNo(VNC));
				
				string strOpen = "";
				string strClosed = "";
							
				foreach (int p in OpenPorts)
				{
					strOpen += p + ", ";
				}
							
				foreach (int p in ClosedPorts)
				{
					strClosed += p + ", ";
				}
							
				listViewItem.SubItems.Add(strOpen.Substring(0, strOpen.Length > 0 ? strOpen.Length - 2 : strOpen.Length));
				listViewItem.SubItems.Add(strClosed.Substring(0, strClosed.Length > 0 ? strClosed.Length - 2 : strClosed.Length));

				return listViewItem;
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddExceptionStackTrace("Tools.PortScan.ToListViewItem() failed.", ex);
				return null;
			}
		}
				
		private static string BoolToYesNo(bool value)
		{
		    return value ? Language.strYes : Language.strNo;
		}

	    public void SetAllProtocols(bool value)
		{
			VNC = value;
			Telnet = value;
			SSH = value;
			Rlogin = value;
			RDP = value;
			HTTPS = value;
			HTTP = value;
		}
        #endregion
	}
			
	public class Scanner
	{
        #region Private Members
		private List<IPAddress> _ipAddresses = new List<IPAddress>();
		private List<int> _ports = new List<int>();
		private Thread _scanThread;
		private List<ScanHost> _scannedHosts = new List<ScanHost>();
        #endregion
				
        #region Public Methods
	
		public Scanner(IPAddress ipAddress1, IPAddress ipAddress2, int port1, int port2)
		{
            IPAddress ipAddressStart = IpAddressMin(ipAddress1, ipAddress2);
            IPAddress ipAddressEnd = IpAddressMax(ipAddress1, ipAddress2);

            int portStart = Math.Min(port1, port2);
			int portEnd = Math.Max(port1, port2);
					
			_ports.Clear();
			for (int port = portStart; port <= portEnd; port++)
			{
				_ports.Add(port);
			}
            _ports.AddRange(new[] { ScanHost.SSHPort, ScanHost.TelnetPort, ScanHost.HTTPPort, ScanHost.HTTPSPort, ScanHost.RloginPort, ScanHost.RDPPort, ScanHost.VNCPort });

            _ipAddresses.Clear();
            _ipAddresses.AddRange(IpAddressArrayFromRange(ipAddressStart, ipAddressEnd));

            _scannedHosts.Clear();
        }
				
		public void StartScan()
		{
			_scanThread = new Thread(ScanAsync);
			_scanThread.SetApartmentState(ApartmentState.STA);
			_scanThread.IsBackground = true;
			_scanThread.Start();
		}
				
		public void StopScan()
		{
			_scanThread.Abort();
        }
				
		public static bool IsPortOpen(string hostname, string port)
		{
			try
			{
				System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient(hostname, Convert.ToInt32(port));
                tcpClient.Close(); 
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
        #endregion

        #region Private Methods

        private int hostCount;
        private void ScanAsync()
		{
			try
			{
			    hostCount = 0;
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, $"Tools.PortScan: Starting scan of {_ipAddresses.Count} hosts...", true);
                foreach (IPAddress ipAddress in _ipAddresses)
				{
                    BeginHostScanEvent?.Invoke(ipAddress.ToString());

                    Ping pingSender = new Ping();

                    try
                    {
                        pingSender.PingCompleted += PingSender_PingCompleted;
                        pingSender.SendAsync(ipAddress, ipAddress);
                    }
                    catch (Exception ex)
                    {
                        Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, $"Tools.PortScan: Ping failed for {ipAddress} {Environment.NewLine} {ex.Message}", true);
                    }
                }
            }
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, $"StartScanBG failed (Tools.PortScan) {Environment.NewLine} {ex.Message}", true);
			}
		}

        /* Some examples found here:
         * http://stackoverflow.com/questions/2114266/convert-ping-application-to-multithreaded-version-to-increase-speed-c-sharp
         */
        private void PingSender_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            // UserState is the IP Address
            var ip = e.UserState.ToString();
            ScanHost scanHost = new ScanHost(ip);
            hostCount++;

            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, $"Tools.PortScan: Scanning {hostCount} of {_ipAddresses.Count} hosts: {scanHost.HostIp}", true);

            if (e.Error != null)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, $"Ping failed to {e.UserState} {Environment.NewLine} {e.Error.Message}", true);
                scanHost.ClosedPorts.AddRange(_ports);
                scanHost.SetAllProtocols(false);
            }
            else if (e.Reply.Status == IPStatus.Success)
            {
                /* ping was successful, try to resolve the hostname */
                try
                {
                    scanHost.HostName = Dns.GetHostEntry(scanHost.HostIp).HostName;
                }
                catch (Exception dnsex)
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                        $"Tools.PortScan: Could not resolve {scanHost.HostIp} {Environment.NewLine} {dnsex.Message}",
                        true);
                }

                if (string.IsNullOrEmpty(scanHost.HostName))
                {
                    scanHost.HostName = scanHost.HostIp;
                }

                foreach (int port in _ports)
                {
                    bool isPortOpen;
                    try
                    {
                        System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient(ip, port);
                        isPortOpen = true;
                        scanHost.OpenPorts.Add(port);
                        tcpClient.Close();
                    }
                    catch (Exception)
                    {
                        isPortOpen = false;
                        scanHost.ClosedPorts.Add(port);
                    }

                    if (port == ScanHost.SSHPort)
                    {
                        scanHost.SSH = isPortOpen;
                    }
                    else if (port == ScanHost.TelnetPort)
                    {
                        scanHost.Telnet = isPortOpen;
                    }
                    else if (port == ScanHost.HTTPPort)
                    {
                        scanHost.HTTP = isPortOpen;
                    }
                    else if (port == ScanHost.HTTPSPort)
                    {
                        scanHost.HTTPS = isPortOpen;
                    }
                    else if (port == ScanHost.RloginPort)
                    {
                        scanHost.Rlogin = isPortOpen;
                    }
                    else if (port == ScanHost.RDPPort)
                    {
                        scanHost.RDP = isPortOpen;
                    }
                    else if (port == ScanHost.VNCPort)
                    {
                        scanHost.VNC = isPortOpen;
                    }
                }
            }
            else if(e.Reply.Status != IPStatus.Success)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, $"Ping did not complete to {e.UserState} : {e.Reply.Status}", true);
                scanHost.ClosedPorts.AddRange(_ports);
                scanHost.SetAllProtocols(false);
            }

            // cleanup
            var p = (Ping)sender;
            p.PingCompleted -= PingSender_PingCompleted;
            p.Dispose();

            var h = string.IsNullOrEmpty(scanHost.HostName) ? "HostNameNotFound" : scanHost.HostName;
            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, $"Tools.PortScan: Scan of {scanHost.HostIp} ({h}) complete.", true);

            _scannedHosts.Add(scanHost);
            HostScannedEvent?.Invoke(scanHost, hostCount, _ipAddresses.Count);

            if (_scannedHosts.Count == _ipAddresses.Count)
                ScanCompleteEvent?.Invoke(_scannedHosts);
        }
        
		private static IPAddress[] IpAddressArrayFromRange(IPAddress ipAddress1, IPAddress ipAddress2)
		{
			IPAddress startIpAddress = IpAddressMin(ipAddress1, ipAddress2);
			IPAddress endIpAddress = IpAddressMax(ipAddress1, ipAddress2);
					
			int startAddress = IpAddressToInt32(startIpAddress);
			int endAddress = IpAddressToInt32(endIpAddress);
			int addressCount = endAddress - startAddress;
					
			IPAddress[] addressArray = new IPAddress[addressCount + 1];
			int index = 0;
			for (int address = startAddress; address <= endAddress; address++)
			{
				addressArray[index] = IpAddressFromInt32(address);
				index++;
			}
					
			return addressArray;
		}
				
		private static IPAddress IpAddressMin(IPAddress ipAddress1, IPAddress ipAddress2)
		{
			if (IpAddressCompare(ipAddress1, ipAddress2) < 0) // ipAddress1 < ipAddress2
			{
				return ipAddress1;
			}
			else
			{
				return ipAddress2;
			}
		}
				
		private static IPAddress IpAddressMax(IPAddress ipAddress1, IPAddress ipAddress2)
		{
			if (IpAddressCompare(ipAddress1, ipAddress2) > 0) // ipAddress1 > ipAddress2
			{
				return ipAddress1;
			}
			else
			{
				return ipAddress2;
			}
		}
				
		private static int IpAddressCompare(IPAddress ipAddress1, IPAddress ipAddress2)
		{
			return IpAddressToInt32(ipAddress1) - IpAddressToInt32(ipAddress2);
		}
				
		private static int IpAddressToInt32(IPAddress ipAddress)
		{
			if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
			{
				throw (new ArgumentException("ipAddress"));
			}
					
			byte[] addressBytes = ipAddress.GetAddressBytes(); // in network order (big-endian)
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(addressBytes); // to host order (little-endian)
			}
			Debug.Assert(addressBytes.Length == 4);
					
			return BitConverter.ToInt32(addressBytes, 0);
		}
				
		private static IPAddress IpAddressFromInt32(int ipAddress)
		{
			byte[] addressBytes = BitConverter.GetBytes(ipAddress); // in host order
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(addressBytes); // to network order (big-endian)
			}
			Debug.Assert(addressBytes.Length == 4);

            return new IPAddress(addressBytes);
		}
        #endregion
				
        #region Events
		public delegate void BeginHostScanEventHandler(string host);
		private BeginHostScanEventHandler BeginHostScanEvent;
				
		public event BeginHostScanEventHandler BeginHostScan
		{
			add
			{
				BeginHostScanEvent = (BeginHostScanEventHandler) Delegate.Combine(BeginHostScanEvent, value);
			}
			remove
			{
				BeginHostScanEvent = (BeginHostScanEventHandler) Delegate.Remove(BeginHostScanEvent, value);
			}
		}
				
		public delegate void HostScannedEventHandler(ScanHost scanHost, int scannedHostCount, int totalHostCount);
		private HostScannedEventHandler HostScannedEvent;
				
		public event HostScannedEventHandler HostScanned
		{
			add
			{
				HostScannedEvent = (HostScannedEventHandler) Delegate.Combine(HostScannedEvent, value);
			}
			remove
			{
				HostScannedEvent = (HostScannedEventHandler) Delegate.Remove(HostScannedEvent, value);
			}
		}
				
		public delegate void ScanCompleteEventHandler(List<ScanHost> hosts);
		private ScanCompleteEventHandler ScanCompleteEvent;
				
		public event ScanCompleteEventHandler ScanComplete
		{
			add
			{
				ScanCompleteEvent = (ScanCompleteEventHandler) Delegate.Combine(ScanCompleteEvent, value);
			}
			remove
			{
				ScanCompleteEvent = (ScanCompleteEventHandler) Delegate.Remove(ScanCompleteEvent, value);
			}
		}
        #endregion
	}
}
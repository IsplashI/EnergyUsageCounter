using EnergyUsageCounter;
using LibreHardwareMonitor.Hardware;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Text.Json;

public class PowerUsageMonitor : IDisposable
{
    private Stopwatch stopwatch;
    private readonly Computer computer;
    private static DateTime lastUpdateTime;
    private static double totalEnergyUsed;
    private string currentDay;
    private Dictionary<ISensor, SensorData> sensorData;
    public Dictionary<string, double> energyUsageHistory; 
    public  string EnergyUsageFilePath = "energy_usage_data.json";
    private class SensorData
    {
        public double Sum { get; set; } = 0.0;
        public int Count { get; set; } = 0;
    }
    private static PowerUsageMonitor instance;
    public static PowerUsageMonitor Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PowerUsageMonitor();
            }
            return instance;
        }
    }

    public PowerUsageMonitor()
    {
        computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsBatteryEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true,
            IsPsuEnabled = true

        };
        computer.Open();
        stopwatch = new Stopwatch();
        totalEnergyUsed = 0.0;
        sensorData = new Dictionary<ISensor, SensorData>();
        energyUsageHistory = new Dictionary<string, double>();

        EnergyUsageFilePath = $"energy_usage_data_{GetDiskSerialNumber()}{GetMacAddress()}.json";
        Console.WriteLine($"{GetDiskSerialNumber()}<<<<<<<<<<<{GetMacAddress()}");
        LoadEnergyUsageHistory();

        if (!HasPowerSensors())
        {
            Console.WriteLine("No power sensors available. Exiting...");
            return; 
        }
    }

    public void StartMonitoring()
    {
        lastUpdateTime = DateTime.Now;
        stopwatch.Restart();
        totalEnergyUsed = 0.0;
    }

    public string GetCurrentPowerUsage()
    {
        var totalPower = GetTotalPowerUsage();
        return $"Current power usage: {totalPower:F2} watts";
    }
    private bool HasPowerSensors()
    {
        foreach (var hardware in computer.Hardware)
        {
            hardware.Update();
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Power && sensor.Value.HasValue)
                {
                    return true; // Знайдено принаймні один датчик потужності
                }
            }
        }
        return false; // Не знайдено жодного датчика потужності
    }
    public void UpdatePowerUsage()
    {
        var currentPower = GetTotalPowerUsage();
        var elapsedTimeInSeconds = (DateTime.Now - lastUpdateTime).TotalSeconds;

        double energyUsed = currentPower * elapsedTimeInSeconds / 3600.0;
        totalEnergyUsed += energyUsed;

        string currentDay = DateTime.Now.ToString("yyyy-MM-dd");
        if (energyUsageHistory.ContainsKey(currentDay))
        {
            energyUsageHistory[currentDay] += energyUsed;
        }
        else
        {
            energyUsageHistory[currentDay] = energyUsed;
        }

        SaveEnergyUsageHistory(); 

        lastUpdateTime = DateTime.Now;
    }

    private void LoadEnergyUsageHistory()
    {
        if (File.Exists(EnergyUsageFilePath))
        {
            var jsonString = File.ReadAllText(EnergyUsageFilePath);
            energyUsageHistory = JsonConvert.DeserializeObject<Dictionary<string, double>>(jsonString);
        }
    }
    private void SaveEnergyUsageHistory()
    {
        JsonSerializerSettings options = new JsonSerializerSettings();
        options.Formatting = Formatting.Indented;
        var jsonString = JsonConvert.SerializeObject(energyUsageHistory, options);
        File.WriteAllText(EnergyUsageFilePath, jsonString);
    }
    public string GetTotalEnergyUsed()
    {
        double totalEnergyUsedToday = energyUsageHistory[DateTime.Now.ToString("yyyy-MM-dd")];
        return $"Energy used from start: {totalEnergyUsed:F2} Wh\nEnergy used today: {totalEnergyUsedToday:F2} Wh";
    }
    private double GetTotalPowerUsage()
    {
        double totalPower = 0.0;

        foreach (var hardware in computer.Hardware)
        {
            hardware.Update();
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Power)
                {
                    if (sensor.Value.HasValue)
                    {
                        if (!sensorData.ContainsKey(sensor))
                        {
                            sensorData[sensor] = new SensorData();
                        }
                        sensorData[sensor].Sum += sensor.Value.Value;
                        sensorData[sensor].Count++;

                        totalPower += sensor.Value.Value;
                    }
                    if (sensorData.TryGetValue(sensor, out SensorData data) && data.Count > 0 && sensor.Value.Value == 0)
                    {
                        totalPower += data.Sum / data.Count;
                    }
                }
            }
        }

        return totalPower;
    }
    public List<string> GetSensorInformation()
    {
        var sensorInfo = new List<string>();

        foreach (var hardware in computer.Hardware)
        {
            hardware.Update();
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Power && sensor.Value.HasValue)
                {
                    string info = $"Hardware: {hardware.HardwareType}, Sensor: {sensor.Name}, Value: {sensor.Value:F2}W";
                    sensorInfo.Add(info);
                }
            }
        }

        return sensorInfo;
    }

    public void Dispose()
    {
        computer.Close();
    }

    public static void RunPowerUsageMonitor()
    {
        
        Instance.StartMonitoring(); // Початок моніторингу

        // Виводимо інформацію про датчики, з яких вдалося зчитати дані
        
        while (true)
        {
            Thread.Sleep(1000); 

            Instance.UpdatePowerUsage(); 
            var sensorInfo = Instance.GetSensorInformation();
            Console.WriteLine("Detected Sensors:");
            foreach (var info in sensorInfo)
            {
                Console.WriteLine(info);
            }
            string currentUsage = Instance.GetCurrentPowerUsage();
            Console.WriteLine(currentUsage);
            string totalEnergyUsed = Instance.GetTotalEnergyUsed();
            Console.WriteLine(totalEnergyUsed);            
        }        
    }
    public static async Task RunPowerMonitors()
    {
        Task powerMonitorTask = Task.Run(() => RunPowerUsageMonitor());
        await Task.CompletedTask;
    }
    public static string GetPowerUsageString()
    {
        Instance.UpdatePowerUsage();
        var sensorInfo = Instance.GetSensorInformation();
        string message = "Detected Sensors:\n";
        foreach (var info in sensorInfo)
        {
            message += info + "\n";
        }
        Console.WriteLine(message);
       
        return $"{message}\n{Instance.GetCurrentPowerUsage()}\n{Instance.GetTotalEnergyUsed()}";
    }
    public string GetDiskSerialNumber()
    {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
        foreach (ManagementObject disk in searcher.Get())
        {
            return disk["SerialNumber"].ToString();
        }
        return null;
    }
    public string GetMacAddress()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault();
    }
}
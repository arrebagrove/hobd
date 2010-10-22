﻿using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using Fleux.Core;

namespace hobd
{
    sealed class HOBD
    {
        public static ConfigData config;
        public static Engine engine;
        public static SensorRegistry Registry;
        public static HOBDTheme theme;
        
        public static string Version {
            get{ 
                return "0.1";
            }
        }

        static string appPath;
        public static string AppPath {
            get{
                if (appPath == null){
                    appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                    if (appPath.StartsWith("file:\\")) appPath = appPath.Substring(6);
                }
                return appPath;
            }
        }
        
        [STAThread]
        private static void Main(string[] args)
        {

            try{
                try{
                    config = new ConfigData(Path.Combine(HOBD.AppPath, "config.xml"));
                }catch(Exception e){
                    Logger.error("HOBD", "failure loading config.xml, using defaults", e);
                    config = new ConfigData();
                }
                
                Logger.SetLevel(config.LogLevel);
                
                var vehicle = config.GetVehicle(config.Vehicle);
                
                if (vehicle == null){
                    Logger.error("HOBD", "Bad configuration: can't find vehicle " + config.Vehicle);
                    vehicle = ConfigVehicleData.defaultVehicle;
                }
                
                HOBD.engine = (Engine)Activator.CreateInstance(null, vehicle.ECUEngine).Unwrap();
                
                IStream stream = null;
                if (config.Port.StartsWith("btspp"))
                    stream = new BluetoothStream();
                else
                    stream = new SerialStream();
                
                engine.Init(stream, config.Port);
                                
                Registry = new SensorRegistry();
                vehicle.Sensors.ForEach((s) =>
                        {
                            Logger.trace("HOBD", "RegisterProvider: "+ s);
                            Registry.RegisterProvider((SensorProvider)Activator.CreateInstance(null, s).Unwrap());
                        });
                
                engine.Registry = Registry;
                engine.Activate();
                
                int dpi_value;
                dpi_value = 96 / (Screen.PrimaryScreen.Bounds.Height / 278);
                if (config.DPI != 0)
                    dpi_value = config.DPI;
                FleuxApplication.TargetDesignDpi = dpi_value;
                HOBD.theme = (HOBDTheme)Activator.CreateInstance(null, config.Theme).Unwrap();                
                
                FleuxApplication.Run(new HomePage());
                
                engine.Deactivate();
                config.Save();
            }catch(Exception e){
                Logger.error("HOBD", "fatal failure, exiting", e);
            }

        }
        
    }
}

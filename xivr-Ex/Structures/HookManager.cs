using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace xivr.Structures
{
    public delegate void HandleStatusDelegate(bool status);

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class HandleStatus : System.Attribute
    {
        public string fnName { get; private set; }
        public HandleStatus(string name)
        {
            fnName = name;
        }
    }

    public class HookManager
    {
        protected Dictionary<string, HandleStatusDelegate> functionList = new Dictionary<string, HandleStatusDelegate>();

        public void SetFunctionHandles(xivr_hooks self, bool doDebug = false)
        {
            //----
            // Gets a list of all the methods the given class contains that are public and instanced (non static)
            // then looks for a specific attirbute attached to the class
            // Once found, create a delegate and add both the attribute and delegate to a dictionary
            //----
            if (doDebug)
                PluginLog.Log("HookManager: Finding Functions");
            functionList.Clear();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            foreach (MethodInfo method in self.GetType().GetMethods(flags))
            {
                foreach (System.Attribute attribute in method.GetCustomAttributes(typeof(HandleStatus), false))
                {
                    string key = ((HandleStatus)attribute).fnName;
                    HandleStatusDelegate handle = (HandleStatusDelegate)HandleStatusDelegate.CreateDelegate(typeof(HandleStatusDelegate), self, method);

                    if (doDebug)
                        PluginLog.Log($"HookManager: Found {key}");
                    if (!functionList.ContainsKey(key))
                        functionList.Add(key, handle);
                }
            }
            if (doDebug)
                PluginLog.Log("HookManager: Found Functions");
        }

        public void EnableFunctionHandles(bool doDebug = false)
        {
            //----
            // Enable all hooks
            //----
            if(doDebug)
                PluginLog.Log("HookManager: Enabling All Functions");
            foreach (KeyValuePair<string, HandleStatusDelegate> attrib in functionList)
                attrib.Value(true);
            if (doDebug)
                PluginLog.Log("HookManager: Enabled All Functions");
        }

        public void DisableFunctionHandles(bool doDebug = false)
        {
            //----
            // Disable all hooks
            //----
            if (doDebug)
                PluginLog.Log("HookManager: Disabling All Functions");
            foreach (KeyValuePair<string, HandleStatusDelegate> attrib in functionList)
                attrib.Value(false);
            if (doDebug)
                PluginLog.Log("HookManager: Disabled All Functions");
        }
    }
}

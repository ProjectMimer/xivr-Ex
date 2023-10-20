using System;
using System.Collections.Generic;
using System.Reflection;


namespace xivr.Structures
{
    public delegate void HandleStatusDelegate(bool status, bool dispose);

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
                Plugin.Log!.Info("HookManager: Finding Functions Start");
            functionList.Clear();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            foreach (MethodInfo method in self.GetType().GetMethods(flags))
            {
                foreach (System.Attribute attribute in method.GetCustomAttributes(typeof(HandleStatus), false))
                {
                    string key = ((HandleStatus)attribute).fnName;
                    HandleStatusDelegate handle = (HandleStatusDelegate)HandleStatusDelegate.CreateDelegate(typeof(HandleStatusDelegate), self, method);

                    if (doDebug)
                        Plugin.Log!.Info($"HookManager: Found {key}");
                    if (!functionList.ContainsKey(key))
                        functionList.Add(key, handle);
                }
            }
            if (doDebug)
                Plugin.Log!.Info("HookManager: Finding Functions End");
        }

        public void EnableFunctionHandles(bool doDebug = false)
        {
            //----
            // Enable all hooks
            //----
            if (doDebug)
            {
                Plugin.Log!.Info("HookManager: Enabling All Functions");
                foreach (KeyValuePair<string, HandleStatusDelegate> attrib in functionList)
                {
                    Plugin.Log!.Info($"HookManager: Enabling {attrib.Key}");
                    attrib.Value(true, false);
                }
                Plugin.Log!.Info("HookManager: Enabled All Functions");
            }
            else
                foreach (KeyValuePair<string, HandleStatusDelegate> attrib in functionList)
                    attrib.Value(true, false);
        }

        public void DisableFunctionHandles(bool doDebug = false)
        {
            //----
            // Disable all hooks
            //----
            if (doDebug)
            {
                Plugin.Log!.Info("HookManager: Disabling All Functions");
                foreach (KeyValuePair<string, HandleStatusDelegate> attrib in functionList)
                {
                    Plugin.Log!.Info($"HookManager: Disabling {attrib.Key}");
                    attrib.Value(false, false);
                }
                Plugin.Log!.Info("HookManager: Disabled All Functions");
            }
            else
                foreach (KeyValuePair<string, HandleStatusDelegate> attrib in functionList)
                    attrib.Value(false, false);
        }

        public void DisposeFunctionHandles(bool doDebug = false)
        {
            //----
            // Disable all hooks
            //----
            if (doDebug)
            {
                Plugin.Log!.Info("HookManager: Disposing All Functions");
                foreach (KeyValuePair<string, HandleStatusDelegate> attrib in functionList)
                {
                    Plugin.Log!.Info($"HookManager: Disposing {attrib.Key}");
                    attrib.Value(false, true);
                }
                Plugin.Log!.Info("HookManager: Disposed All Functions");
            }
            else
                foreach (KeyValuePair<string, HandleStatusDelegate> attrib in functionList)
                    attrib.Value(false, true);
        }
    }
}

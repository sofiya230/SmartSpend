
namespace OfxSharpLib {
    using System;
    
    
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("OfxSharpLib.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        internal static string BankAccount {
            get {
                return ResourceManager.GetString("BankAccount", resourceCulture);
            }
        }
        
        internal static string CCAccount {
            get {
                return ResourceManager.GetString("CCAccount", resourceCulture);
            }
        }
        
        internal static string InsufficentFunds {
            get {
                return ResourceManager.GetString("InsufficentFunds", resourceCulture);
            }
        }
        
        internal static string NoFunds {
            get {
                return ResourceManager.GetString("NoFunds", resourceCulture);
            }
        }
        
        internal static string NoMoney {
            get {
                return ResourceManager.GetString("NoMoney", resourceCulture);
            }
        }
        
        internal static string SignOn {
            get {
                return ResourceManager.GetString("SignOn", resourceCulture);
            }
        }
    }
}

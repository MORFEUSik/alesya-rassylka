﻿#pragma checksum "..\..\..\SelectRecipientWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "5DE7AA57AA35B7E46036B7D8AAE13B62DE87C980"
//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Chromes;
using Xceed.Wpf.Toolkit.Converters;
using Xceed.Wpf.Toolkit.Core;
using Xceed.Wpf.Toolkit.Core.Converters;
using Xceed.Wpf.Toolkit.Core.Input;
using Xceed.Wpf.Toolkit.Core.Media;
using Xceed.Wpf.Toolkit.Core.Utilities;
using Xceed.Wpf.Toolkit.Mag.Converters;
using Xceed.Wpf.Toolkit.Panels;
using Xceed.Wpf.Toolkit.Primitives;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Xceed.Wpf.Toolkit.PropertyGrid.Commands;
using Xceed.Wpf.Toolkit.PropertyGrid.Converters;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Xceed.Wpf.Toolkit.Zoombox;
using alesya_rassylka;


namespace alesya_rassylka {
    
    
    /// <summary>
    /// SelectRecipientWindow
    /// </summary>
    public partial class SelectRecipientWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 159 "..\..\..\SelectRecipientWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox CategoryListBox;
        
        #line default
        #line hidden
        
        
        #line 181 "..\..\..\SelectRecipientWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox CategorySearchTextBox;
        
        #line default
        #line hidden
        
        
        #line 187 "..\..\..\SelectRecipientWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox RecipientsListBox;
        
        #line default
        #line hidden
        
        
        #line 212 "..\..\..\SelectRecipientWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SearchTextBox;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.3.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/alesya-rassylka;component/selectrecipientwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\SelectRecipientWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.3.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.CategoryListBox = ((System.Windows.Controls.ListBox)(target));
            
            #line 164 "..\..\..\SelectRecipientWindow.xaml"
            this.CategoryListBox.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.CategoryListBox_SelectionChanged);
            
            #line default
            #line hidden
            
            #line 165 "..\..\..\SelectRecipientWindow.xaml"
            this.CategoryListBox.PreviewMouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.CategoryListBox_PreviewMouseRightButtonDown);
            
            #line default
            #line hidden
            return;
            case 2:
            
            #line 173 "..\..\..\SelectRecipientWindow.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.AddCategory_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            
            #line 174 "..\..\..\SelectRecipientWindow.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.DeleteCategory_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            
            #line 175 "..\..\..\SelectRecipientWindow.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.EditCategory_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.CategorySearchTextBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 184 "..\..\..\SelectRecipientWindow.xaml"
            this.CategorySearchTextBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.CategorySearchTextBox_TextChanged);
            
            #line default
            #line hidden
            return;
            case 6:
            this.RecipientsListBox = ((System.Windows.Controls.ListBox)(target));
            
            #line 190 "..\..\..\SelectRecipientWindow.xaml"
            this.RecipientsListBox.PreviewMouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.RecipientsListBox_PreviewMouseRightButtonDown);
            
            #line default
            #line hidden
            return;
            case 7:
            
            #line 203 "..\..\..\SelectRecipientWindow.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.AddRecipient_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            
            #line 204 "..\..\..\SelectRecipientWindow.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.DeleteRecipient_Click);
            
            #line default
            #line hidden
            return;
            case 9:
            
            #line 205 "..\..\..\SelectRecipientWindow.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.EditRecipient_Click);
            
            #line default
            #line hidden
            return;
            case 10:
            this.SearchTextBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 215 "..\..\..\SelectRecipientWindow.xaml"
            this.SearchTextBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.SearchTextBox_TextChanged);
            
            #line default
            #line hidden
            return;
            case 11:
            
            #line 218 "..\..\..\SelectRecipientWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ToggleSelection_Click);
            
            #line default
            #line hidden
            return;
            case 12:
            
            #line 223 "..\..\..\SelectRecipientWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ConfirmSelection_Click);
            
            #line default
            #line hidden
            return;
            case 13:
            
            #line 224 "..\..\..\SelectRecipientWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Cancel_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}


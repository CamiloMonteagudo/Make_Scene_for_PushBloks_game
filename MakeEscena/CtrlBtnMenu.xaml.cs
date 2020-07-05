using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MakeEscena
  {
  //==============================================================================================================================================================
  /// Interaction logic for TestControl.xaml
  public partial class CtrlBtnMenu : UserControl
    {
    bool check = false;

    SolidColorBrush CheckBrush  = new SolidColorBrush( Color.FromArgb( 64, 0 , 49, 113 ) );
    SolidColorBrush NormalBrush = null;

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public CtrlBtnMenu()
      {
      InitializeComponent(); 

      BotonMenu.Background  = NormalBrush;
      BotonMenu.BorderBrush = NormalBrush;

      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public ImageSource Source{ get{ return BtnMenuImg.Source; } set{ BtnMenuImg.Source = value; } }

    public string Text{ get{ return BtnMenuTxt.Text; } set{ BtnMenuTxt.Text = value; } }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public bool Check { 
                      get { return check; } 
                      set { 
                          if( value )
                            { 
                            BotonMenu.Background  = CheckBrush;
                            BotonMenu.BorderBrush = CheckBrush;
                            }
                          else
                            {
                            BotonMenu.Background  = NormalBrush;
                            BotonMenu.BorderBrush = NormalBrush;
                            }
                          check = value; 
                          } 
                      }

    }//============================================================================================================================================================
  }

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
  // Interaction logic for CtrlBlock.xaml
  public partial class CtrlBlock : UserControl
    {
    public Brush bckgd = new SolidColorBrush( Color.FromArgb( 0,0,0,0 ) );

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public CtrlBlock( int col, int row )
      {
      InitializeComponent();

      Tag = DrawTipo.Block;

      Canvas.SetLeft  ( this, Escena.GetX(col) );
      Canvas.SetTop   ( this, Escena.GetY(row) );
      Canvas.SetZIndex( this, 300 ); 

      Width = Height = Escena.zCell;
      ShowId();

      bckgd = Frame.Background;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public void ShowId()
      {
      Info.Visibility = (Escena.ShowID)? Visibility.Visible : Visibility.Hidden;
      }

    }//============================================================================================================================================================
  }

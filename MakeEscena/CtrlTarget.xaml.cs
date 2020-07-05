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
  // Interaction logic for CtrlTarget.xaml
  public partial class CtrlTarget : UserControl
    {
    public int xCell;
    public int yCell;
    
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    int id;
    public int ID { 
                  get{ return id;} 
                  set{ id = value; Info.Text = id.ToString(); } 
                  }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public CtrlTarget( int col, int row, int Id )
      {
      InitializeComponent();

      Tag = DrawTipo.Target;

      xCell = col;
      yCell = row;

      Canvas.SetLeft  ( this, Escena.GetX(col) );
      Canvas.SetTop   ( this, Escena.GetY(row) );
      Canvas.SetZIndex( this, 200 ); 

      ID = Id;
      ReDraw();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public void ReDraw()
      {
      int size = Escena.zCell;
      Width = Height = size;

      int Sep = size/8;
      int R   = size/2 - Sep;
 
      Circle1.Width = Circle1.Height = 2*R;
      Circle1.CornerRadius = new CornerRadius( R );

      Canvas.SetLeft( Circle1, Sep );
      Canvas.SetTop ( Circle1, Sep );

      Sep *= 2;
      R   = size/2 - Sep;
 
      Circle2.Width = Circle2.Height = 2*R;
      Circle2.CornerRadius = new CornerRadius( R );

      Canvas.SetLeft( Circle2, Sep );
      Canvas.SetTop ( Circle2, Sep );

      Info.Text = id.ToString();

      Show();
      ShowId();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public void Show()
      {
      var vis = (Escena.ShowTarget)? Visibility.Visible : Visibility.Hidden;

      Circle1.Visibility = vis;
      Circle2.Visibility = vis;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public void ShowId()
      {
      Info.Visibility = (Escena.ShowID)? Visibility.Visible : Visibility.Hidden;
      }

    }//============================================================================================================================================================
  }

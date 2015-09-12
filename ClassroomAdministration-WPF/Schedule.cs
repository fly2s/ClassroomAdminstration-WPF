﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClassroomAdministration_WPF
{

    class Schedule
    {
		
		RentTableOwner Owner;

		Grid GridScheduleHead; 
		Grid GridSchedule; Label RectangleChosonClass;

        WindowIndex Father;

        public RentTable RentTable { get { return Owner.RentTable; } }
        public Rent ChosenRent { get { return chosenRent; } }

        public Schedule(RentTableOwner owner, Grid gridSchdule, Label rectangleChosonClass, Grid gridScheduleHead, WindowIndex father)
        {
            Owner = owner;
            
            GridSchedule = gridSchdule;
            GridScheduleHead = gridScheduleHead;
            RectangleChosonClass = rectangleChosonClass;

            Father = father;

            headInitialize();
            checkoutWeek();

            if (owner != null)
            {
                Reset();
            }

            GridSchedule.PreviewMouseDown += GridSchedule_PreviewMouseDown;
        }

        void GridSchedule_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Father.GridFocus();

            Point pos = e.GetPosition(GridSchedule);
            double aCol = GridSchedule.ActualWidth, aRow = GridSchedule.ActualHeight;

            int col = (int)(pos.X / aCol * cntCol), row = (int)(pos.Y / aRow * cntRow);

            Father.GoToDateClass(col, row + 1);
        }

        public void ChangeOwner(RentTableOwner owner)
        {
            Owner = owner;

            Reset();
        }
        public void Reset()
        {
            if (Owner == null) return;

            Owner.GetMyRentTable();

            ScheduleInitialize(GridSchedule, Owner.RentTable, TextBlockRents, RectangleChosonClass);
        }


        //课表尺寸
        const int cntCol = 7, cntRow = 14;

        //初始日期
        DateTime firstDate = RentTime.FirstDate;
        //星期表头
        Label[] head;
        string[] weekDayName = RentTime.weekDayName;
        
        //课程TextBlock列表
        public List<TextBlock> TextBlockRents = new List<TextBlock>();

        //选中的格子
        Rent chosenRent = null;
        TextBlock TBHighlight = null;

        //初始化星期表头
        private void headInitialize()
        {
            head = new Label[7];
            for (int i = 0; i < 7; ++i)
            {
                head[i] = new Label();
                GridScheduleHead.Children.Add(head[i]);
                head[i].Content = weekDayName[i];
                head[i].VerticalContentAlignment = VerticalAlignment.Center;
                head[i].HorizontalContentAlignment = HorizontalAlignment.Center;
                head[i].SetValue(Grid.ColumnProperty, i);
            }

        }
        //初始化课表
        private void ScheduleInitialize(Grid grid, RentTable rentTable, List<TextBlock> textBlockList, Label chosen)
        {
            foreach (TextBlock tb in textBlockList)
                if (grid.Children.Contains(tb)) grid.Children.Remove(tb);
            textBlockList.Clear();

            foreach (Rent r in rentTable.Rents)
            {
                TextBlock tb = new TextBlock();
                grid.Children.Add(tb);
                textBlockList.Add(tb);

                TextBlockInitialize(tb, r);
            }
            chosen.Visibility = Visibility.Visible;
            if (grid.Children.Contains(chosen)) grid.Children.Remove(chosen);
            grid.Children.Add(chosen);

            SetDateClass(Father.CurrDate, Father.CurrClass);
        }
        //初始化单个课程
        private void TextBlockInitialize(TextBlock tb, Rent r, bool MouseShow = true)
        {
            tb.Tag = r;

            tb.Background = MyColor.NameBrush(r.Info);
            tb.Text = r.Info; if (!r.Approved) tb.Text += "(未审核)";
            Classroom c = Building.GetClassroom(r.cId); if (c != null) tb.Text += ("@" + c.Name);
            tb.FontSize = 16;

            tb.Foreground = new SolidColorBrush(WindowIndex.textColor);
            tb.TextWrapping = TextWrapping.Wrap;


            tb.SetValue(Grid.ColumnProperty, r.Time.WeekDay);
            tb.SetValue(Grid.RowProperty, r.Time.StartClass - 1);
            tb.SetValue(Grid.RowSpanProperty, r.Time.KeepClass);

            tb.MouseDown += tb_MouseDown;

            if (MouseShow)
            {
                tb.MouseEnter += tb_MouseEnter;
                tb.MouseLeave += tb_MouseLeave;
            }
        }

        void tb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Rent r = (sender as TextBlock).Tag as Rent;

            if (Father.MaxBorder.IsEnabled)
                new WindowRent(r, Father).ShowDialog();
            else new WindowRent(r, Father, "big").ShowDialog();

            e.Handled = false;
        }
        void tb_MouseLeave(object sender, MouseEventArgs e)
        {
            TextBlock tb = (TextBlock)sender;
            Rent r = (Rent)(tb.Tag);
            tb.Background = MyColor.NameBrush(r.Info);
        }
        void tb_MouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock tb = (TextBlock)sender;
            Rent r = (Rent)(tb.Tag);
            tb.Background = MyColor.NameBrush(r.Info, 0.8);
        }

        //选择日期时间
        public void SetDateClass(DateTime date, int cc)
        {
            if (Owner == null) return;


            if (cc < 1 || cc > 14) return;

            if (date < firstDate)
            {
                return;
            }

            int days = (Father.CurrDate - firstDate).Days;
            SetWeeks(days / 7 + 1);

            ChosenRentControl();

            RectangleChosonClass.SetValue(Grid.ColumnProperty, Father.CurrWeekDay);
            RectangleChosonClass.SetValue(Grid.RowProperty, Father.CurrClass - 1);

        }

        //设置周数
        public void checkoutWeek()
        {
            if (Owner == null) return;

            ScheduleCheckoutWeek(Owner.RentTable, TextBlockRents);

            ScheduleHeadCheckoutWeek(head);
        }
        public void SetWeeks(int w)
        {
            if (Owner == null) return;

            if (w != Father.CurrWeek)
            {
                checkoutWeek();
            }
        }
        //按照周数更新课程表
        private void ScheduleCheckoutWeek(RentTable rentTable, List<TextBlock> TBList)
        {
            DateTime date = firstDate + new TimeSpan(7 * (Father.CurrWeek - 1), 0, 0, 0);

            if (rentTable == null) return;

            List<Rent> list = rentTable.GetFromWeek(date);

            foreach (TextBlock tb in TBList)
                if (list.Contains((Rent)tb.Tag)) tb.Visibility = Visibility.Visible;
                else tb.Visibility = Visibility.Collapsed;
        }
        private void ScheduleHeadCheckoutWeek(Label[] head)
        {
            for (int i = 0; i < 7; ++i)
            {
                DateTime theDate = firstDate + new TimeSpan(7 * (Father.CurrWeek - 1) + i, 0, 0, 0);
                head[i].Content = theDate.Month + "." + theDate.Day + weekDayName[i];
            }
        }

        //检查选中的时间点的课程
        public void ChosenRentControl()
        {
            if (Owner == null) return;

            chosenRent = Owner.RentTable.GetRentFromDateClass(Father.CurrDate, Father.CurrClass);
            TBHighlight = Hightlight(TBHighlight, chosenRent, GridSchedule);

        }
        private TextBlock Hightlight(TextBlock tbh, Rent r, Grid grid)
        {
            if (grid.Children.Contains(tbh))
            {
                tbh.Visibility = Visibility.Collapsed;
                grid.Children.Remove(tbh);
            }

            if (r == null) return null;

        //    Console.WriteLine("HightLight Rent: " + r.Info);

            tbh = new TextBlock();
            grid.Children.Add(tbh);
            TextBlockInitialize(tbh, r, false);

            tbh.Background = MyColor.NameBrush(r.Info, 1);

            return tbh;
        }

        private TextBlock Rent2Block(Rent r)
        {
            foreach (TextBlock tb in TextBlockRents)
                if ((Rent)tb.Tag == r) return tb;
            return null;
        }

    }
}

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
using System.Threading;
using System.Windows.Threading;
using Rainmeter;

namespace MediaElementNs
{
	/// <summary>
	/// MediaWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MediaWindow : Window
	{
		Rainmeter.API rm;
		readonly SkinWindow owner;

		public MediaWindow(Rainmeter.API rm)
		{
			this.rm = rm;
			owner = new SkinWindow(rm);

			InitializeComponent();
		}


		#region 更新窗口坐标

		bool SnapsToSkin = false;

		double offsetX = 0;
		double offsetY = 0;
		
		int threadCount = 0;

		public delegate void UpdateStatusDelegate();

		async void UpdateStatus()
		{
			if (!SnapsToSkin) return;
			if (threadCount > 1) return;

			if (this.Visibility == System.Windows.Visibility.Visible)
			{
				owner.UpdateStatus();
				//不要rm参数，否则将在皮肤刷新时引起错误
				this.Left = owner.X + offsetX;
				this.Top = owner.Y + offsetY;
				this.Topmost = owner.IsTopmost;
			}

			threadCount++;

			await Task.Delay(50);

			this.Dispatcher.BeginInvoke(
				DispatcherPriority.SystemIdle,
				new UpdateStatusDelegate(UpdateStatus));
			//await可能会在皮肤刷新时引起错误

			threadCount--;
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			threadCount = 99;
			base.OnClosing(e);
		}

		#endregion

		#region 加载节点选项

		static ThicknessConverter thicknessConverter = new ThicknessConverter();

		public void LoadOptions(Rainmeter.API rm = null, Uri source = null)
		{
			offsetX = rm.ReadDouble("X", 0);
			offsetY = rm.ReadDouble("Y", 0);
			Width = rm.ReadDouble("W", 100);
			Height = rm.ReadDouble("H", 200);

			if (rm != null)
			{
				this.rm = rm;
				owner.UpdateStatus(rm);
			}
			else
			{
				rm = this.rm;
			}
			
			string option;

			var snaps = rm.ReadInt("SnapsToSkin", 1) > 0;
			if (snaps && !SnapsToSkin)
			{
 				// 只有当SnapsToSkin==1，且当前SnapsToSkin != true时，
				// 才需要添加UpdateStatus到循环
				this.Dispatcher.BeginInvoke(
					DispatcherPriority.Normal,
					new UpdateStatusDelegate(UpdateStatus));
			}
			SnapsToSkin = snaps;

			#region MediaSource
			if (source == null)
			{
				option = rm.ReadPath("MediaSource", null);
				if (!String.IsNullOrEmpty(option))
				{
					try
					{
						var uri = new Uri(option);
						source = uri;
					}
					catch (Exception ex)
					{
						rm.Error("MediaElement.dll: 无效的媒体URL。" + ex.Message);
					}
				}
			}
			if (source != null && PART_MediaElement.Source != source)
			{
				PART_MediaElement.Close();
				PART_MediaElement.Source = source;
			}
			#endregion

			//Volume
			PART_MediaElement.Volume = rm.ReadDouble("Volume",0.5);

			//SpeedRatio
			PART_MediaElement.SpeedRatio = rm.ReadDouble("SpeedRatio",1);

			//Balance
			PART_MediaElement.Balance = rm.ReadDouble("Balance", 0);


			#region Stretch
			option = rm.ReadString("Stretch", null);
			Stretch stretch;
			if (!String.IsNullOrEmpty(option) &&
				Enum.TryParse<Stretch>(option, true, out stretch))
			{
				PART_MediaElement.Stretch = stretch;
			}
			#endregion

			#region Padding
			option = rm.ReadString("Padding", null);
			if (!String.IsNullOrEmpty(option))
			{
				try
				{
					PART_MediaElement.Margin = (Thickness) thicknessConverter.ConvertFromString(option);
				}
				catch
				{ }
			}
			#endregion

			#region Background
			option = rm.ReadString("Background",null);
			if (!String.IsNullOrEmpty(option))
			{
				var color = ConvertToColor(option);
				if (color == Colors.Transparent)
					this.Background = null; 
				else 
					this.Background = new SolidColorBrush(color);
			}
			#endregion

			//ClickToPlay
			playPauseOnClick = rm.ReadInt("ClickToPlay", 2) > 0;

			//AllowDrop
			AllowDrop = rm.ReadInt("AllowDrop", 1) > 0;



			//异步
			this.Dispatcher.BeginInvoke(
				DispatcherPriority.Normal,
				new UpdateStatusDelegate(LoadBehaviourOptions));
		}

		#endregion

		#region LoadedBehaviour/EndedBehaviour

		void LoadBehaviourOptions()
		{ 
			//记录媒体播放结束时播放的动作
			#region EndedBehaviour

			string option = rm.ReadString("EndedBehaviour", "None").ToLower();

			if (option.IndexOf("dispose") >= 0)
			{
				_EndedBehaviour = EndedBehaviour.Dispose;
			}
			else if (option.IndexOf("repeat") >= 0)
			{
				_EndedBehaviour = EndedBehaviour.Repeat;
				RepeatCount = -1;

				var _loc = option.IndexOf("repeat");
				var count = option.Substring(_loc + 6).TrimStart();
				int _c;
				if (Int32.TryParse(count, out _c))
				{ RepeatCount = _c; }
			}
			else if (option.IndexOf("reverse") >= 0)
			{
				_EndedBehaviour = EndedBehaviour.Reverse;
			}
			else
			{
				_EndedBehaviour = EndedBehaviour.None;
			}
			if (option.IndexOf("hide") >= 0)
			{
				_EndedBehaviour |= EndedBehaviour.Hide;
			}

			#endregion

			//Measure加载完毕后执行的动作
			#region LoadedBehaviour
			
			option = rm.ReadString("LoadedBehaviour", "").ToLower();
			isPlaying = false;

			if (option.IndexOf("activate") >= 0)
			{
				this.Execute("activate");
			}
			else if (option.IndexOf("show") >= 0)
			{
				this.Execute("show");
			}
			if (option.IndexOf("play") >= 0)
			{
				this.Execute("play");
			}
			else if (option.IndexOf("pause") >= 0)
			{
				this.Execute("pause");
			}

			#endregion
		}

		int RepeatCount = -1;

		EndedBehaviour _EndedBehaviour = EndedBehaviour.None;

		enum EndedBehaviour
		{
			None = 0x00,
			Reverse = 0x01,
			Repeat = 0x02,
			Dispose = 0x08,
			Hide = 0xF0,
		}

		#endregion

		#region 处理!CommandMeasure指令
		bool isPlaying = false;

		/// <summary>
		/// 处理 !CommandMeasure
		/// </summary>
		public void Execute(string args)
		{
			args = args.Trim().ToLower();

			if (args == "show")
			{
				if (this.Visibility != Visibility.Visible)
					this.Show();
			}
			else if (args == "hide")
			{
				this.Hide();
			}
			else if (args == "toggle")
			{
				if (this.Visibility == Visibility.Visible)
					this.Hide();
				else
					this.Show();
			}
			else if (args == "activate")
			{
				if (this.Visibility != Visibility.Visible)
					this.Show();
				this.Activate();
			}
			else if (args == "play")
			{
				PART_MediaElement.Play();
				isPlaying = true;
			}
			else if (args == "pause")
			{
				PART_MediaElement.Pause();
				isPlaying = false;
			}
			else if (args == "playpause")
			{
				if (isPlaying)
				{
					PART_MediaElement.Pause();
					isPlaying = false;
				}
				else
				{
					PART_MediaElement.Play();
					isPlaying = true;
				}
			}
			else if (args == "stop")
			{
				PART_MediaElement.Stop();
				isPlaying = false;
			}
			else if (args == "close")
			{
				PART_MediaElement.Close();
				isPlaying = false;
			}
			else if (args.StartsWith("set"))
			{
				if (args.StartsWith("setposition"))
				{
					args = args.Substring(11).TrimStart();
					double num;
					bool addvalue;
					if (TryParseArgument(args,out num, out addvalue))
					{
						TimeSpan val = TimeSpan.FromSeconds(num);
						PART_MediaElement.Position = addvalue
							? PART_MediaElement.Position + val
							: val;
					}
				}
				else if (args.StartsWith("setprogress"))
				{
					args = args.Substring(11).TrimStart();
					double num;
					bool addvalue;
					if (TryParseArgument(args, out num, out addvalue))
					{
						num = num / 100;
						if (PART_MediaElement.NaturalDuration == null ||
							!PART_MediaElement.NaturalDuration.HasTimeSpan)
							return;
						var dur = PART_MediaElement.NaturalDuration.TimeSpan.TotalSeconds;
						var pos = addvalue ?
							dur * num + dur : dur * num;

						pos = Math.Max(0, pos);
						pos = Math.Min(pos, dur);

						PART_MediaElement.Position = TimeSpan.FromSeconds(pos);
					}
				}
			}
		}

		bool TryParseArgument(string arg, out double num, out bool addvalue)
		{
			num = 0;
			addvalue = false;
			try
			{
				if (arg[0] == '+' || arg[0] == '-')
				{
					addvalue = true;
					num = Double.Parse(arg);
					return true;
				}
				else
				{
					num = Double.Parse(arg);
					return true;
				}
			}
			catch { }
			return false;
		}
		#endregion

		#region 返回数值与文本值

		/// <summary>
		/// 返回当前 MediaElement 的播放进度百分比
		/// </summary>
		public double GetPosition()
		{
			try
			{
				var dur = PART_MediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
				var pos = PART_MediaElement.Position.TotalMilliseconds;
				if (dur > 0)
					return pos / dur;
			}
			catch 
			{ }

			return 0.0;
		}

		/// <summary>
		/// 返回当前 MediaElement 的播放URL
		/// </summary>
		public string GetSourceUrl()
		{
			var uri = PART_MediaElement.Source;
			if (uri != null) 
				return uri.ToString();

			return "";
		}

		#endregion

		#region 处理播放事件

		void _MediaOpened(object sender, RoutedEventArgs e)
		{
			doAction("MediaOpenedAction");
		}

		void _MediaEnded(object sender, RoutedEventArgs s)
		{
			isPlaying = false;

			if(_EndedBehaviour.HasFlag(EndedBehaviour.Hide))
			{
				this.Execute("hide");
			}
			if (_EndedBehaviour.HasFlag(EndedBehaviour.Dispose))
			{
				this.Execute("close");
			}
			else if (_EndedBehaviour.HasFlag(EndedBehaviour.Repeat))
			{
				this.Execute("stop");
				if (RepeatCount != 0)
				{
					RepeatCount--;
					this.Execute("play");
				}
			}
			else if (_EndedBehaviour.HasFlag(EndedBehaviour.Reverse))
			{
				this.Execute("stop");
			}

			doAction("MediaEndedAction");
		}

		void _MediaFailed(object sender, ExceptionRoutedEventArgs e)
		{
			isPlaying = false;

			rm.Warning("MediaElement.dll: 播放失败。" + e.ErrorException.Message);

			doAction("MediaFailedAction");
		}

		void doAction(string option)
		{
			var cmd = rm.ReadString(option, null);

			if (!String.IsNullOrEmpty(cmd))
				API.Execute(rm.GetSkin(), cmd);
		}
		#endregion

		#region 处理鼠标点击与拖拽动作
		bool playPauseOnClick = false;

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (playPauseOnClick)
			{
				Execute("playpause");
			}
 			base.OnMouseLeftButtonDown(e);
		}

		#endregion

		#region 字符串转换为颜色
		//static ColorConverter colorConverter = new ColorConverter();
		static Color ConvertToColor(string text)
		{
			var ret = Colors.Transparent;

			string strColor = text.Trim();
			var args = strColor.Split(',');
			if (args.Length >= 3)
			{
				try
				{
					ret.R = Byte.Parse(args[0]);
					ret.G = Byte.Parse(args[1]);
					ret.B = Byte.Parse(args[2]);
					ret.A = 255;
					if (args.Length >= 4)
						ret.A = Byte.Parse(args[3]);

					return ret;
				}
				catch { } 
			}
			else if (strColor.Length > 6)
			{
				try
				{
					ret.R = Byte.Parse(strColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
					ret.G = Byte.Parse(strColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
					ret.B = Byte.Parse(strColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
					ret.A = 255;
					if (strColor.Length >= 8)
						ret.A = Byte.Parse(strColor.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

					return ret;
				}
				catch { }
			}
			else
			{
				//try
				//{
				//	var obj = colorConverter.ConvertFrom(text);
				//	if (obj != null)
				//		return (Color)obj;
				//}
				//catch (Exception ex)
				//{
				//	API.Log(API.LogType.Warning,text + ex.Message);
				//}
				
			}

			return Colors.Transparent;
		}
		#endregion

		#region 实现文件拖放与字符串拖放
		private void Window_Drop(object sender, DragEventArgs e)
		{
			string target = null;

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] data = (string[]) e.Data.GetData(DataFormats.FileDrop);

				if (data != null && data.Length > 0)
					target = data[0];
			}
			else if (e.Data.GetDataPresent(DataFormats.StringFormat, true))
			{
				target = e.Data.GetData(DataFormats.StringFormat, true) as string;
			}

			if (String.IsNullOrEmpty(target)) return;

			Uri uri = null;
			try
			{
				uri = new Uri(target);
			}
			catch
			{
				rm.Error("MediaElement.dll：未能从拖拽目标创建URI：" + target);
			}

			if (uri != null)
			{
				LoadOptions(rm, uri);
			}
		}
		#endregion
	}
}

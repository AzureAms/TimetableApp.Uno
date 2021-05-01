﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace Windows.UI.Popups
{
	public sealed partial class MessageDialog
	{
		private static readonly MethodInfo FromTaskFunction = typeof(IAsyncOperation<IUICommand>).Assembly.GetTypes()
																.FirstOrDefault(type => !type.IsGenericType && type.FullName == "Windows.Foundation.AsyncOperation")
																.GetMethod("FromTask", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(typeof(IUICommand));

		/// <summary>
		/// Creates a new instance of the MessageDialog class, using the specified message content and no title.
		/// </summary>
		/// <param name="content"></param>
		public MessageDialog(string content)
			: this(content, "")
		{
		}

		/// <summary>
		/// Creates a new instance of the MessageDialog class, using the specified message content and title.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="title"></param>
		public MessageDialog(string content, string title)
		{
			if (content == null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			if (title == null)
			{
				throw new ArgumentNullException(nameof(title));
			}

			// They can both be empty.
			Content = content;
			Title = title;

			var collection = new ObservableCollection<IUICommand>();
			collection.CollectionChanged += (s, e) => this.ValidateCommands();

			Commands = collection;
		}

		public uint CancelCommandIndex { get; set; } = uint.MaxValue;
		public IList<IUICommand> Commands { get; }
		public string Content { get; set; }
		public uint DefaultCommandIndex { get; set; } = 0;
		public MessageDialogOptions Options { get; set; }
		public string Title { get; set; }

		public IAsyncOperation<IUICommand> ShowAsync()
        {
			Func<CancellationToken, Task<IUICommand>> func = async (ct) =>
			{
				return await Task.Run(() =>
				{
					TimetableApp.Skia.Wpf.MessageBox.Show(Content, Title);
					return new UICommand("OK") as IUICommand;
				}, ct);
			};
			return FromTaskFunction.Invoke(null, new object[] { func }) as IAsyncOperation<IUICommand>;
        }

		partial void ValidateCommands();
	}
}
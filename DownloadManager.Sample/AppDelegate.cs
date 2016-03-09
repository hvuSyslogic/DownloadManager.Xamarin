﻿using Foundation;
using UIKit;
using MonoTouch.Dialog;
using System.Linq;
using DownloadManager.iOS;
using System;
using ObjCRuntime;
using System.Threading;
using System.Threading.Tasks;
using DownloadManager.iOS.Bo;

namespace DownloadManager.Sample
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register ("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations

		public override UIWindow Window {
			get;
			set;
		}
		public UINavigationController Navigation {
			get;
			set;
		}

		Section _downloads;
		Downloader _downloader;
		DialogViewController _sample;

		public override void HandleEventsForBackgroundUrl (UIApplication application, string sessionIdentifier, System.Action completionHandler)
		{
			_downloader.Completion = completionHandler;
		}

		public override void DidEnterBackground (UIApplication application)
		{
			Console.WriteLine ("[AppDelegate] DidEnterBackground");
		}

		public override void WillEnterForeground (UIApplication application)
		{
			Console.WriteLine ("[AppDelegate] WillEnterForeground");
		}

		public override void PerformFetch (UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
		{
			Console.WriteLine ("[AppDelegate] PerformFetch");

			var result = UIBackgroundFetchResult.NoData;

			try 
			{
				_downloader.Run();
				result = UIBackgroundFetchResult.NewData;
			}
			catch 
			{
				result = UIBackgroundFetchResult.Failed;
			}
			finally
			{
				completionHandler (result);
			}
		}

		public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
		{
			Console.WriteLine ("[AppDelegate] FinishedLaunching");

			UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval (UIApplication.BackgroundFetchIntervalMinimum);
			Window = new UIWindow (UIScreen.MainScreen.Bounds);

			_downloader = new Downloader ();
			_downloads = new Section ("Downloads") {
			}; 

			string templateurl = "http://pokeprice.local/api/v1/card/image/BLW/{0}";
			string zipurl = "http://pokeprice.local/api/v1/card/zip/{0}";
			string httpurl = "http://pokeprice.local/api/v1/http/{0}";
			string redirecturl = "http://pokeprice.local/api/v1/http/redirect/infinite/0";
			string scollections = "AOR,AQ,AR,B2,BCR,BEST,BKT,BLW,BS,CG,CL,DCR,DEX,DF,DP,DR,DRV,DRX,DS,DX,EM,EPO,EX,FFI,FLF,FO,G1,G2,GE,HL,HP,HS,JU,KSS,LA,LC,LM,LTR,MA,MCD2011,MCD2012,MD,MT,N1,N2,N3,N4,NINTENDOBLACK,NVI,NXD,PHF,PK,PL,PLB,PLF,PLS,POP1,POP2,POP3,POP4,POP5,POP6,POP7,POP8,POP9,PR-BLW,PR-DP,PR-HS,PR-XY,PRC,RG,ROS,RR,RS,RU,SF,SI,SK,SS,SV,SW,TM,TR,TRR,UD,UF,UL,VICTORY,WIZARDSBLACK,XY";
			string[] collections = scollections.Split (',');

			var globalprogress = new StringElement ("");
			var root = new RootElement ("Root") {
				new Section ("Management"){
					new BooleanElement ("Enabled", true),
					globalprogress
				},
				_downloads
			};

			_downloader.Progress += (progress) => {
				float percent = (progress.Written / (float)progress.Total) * 100;
				int ipercent = (int)percent;
				string caption = string.Format("{0} {1} {2}% ({3} / {4})", 
					"Global", 
					progress.State.ToString(),
					ipercent,
					progress.Written,
					progress.Total
				);
				InvokeOnMainThread(() => {
					globalprogress.Caption = caption;
					globalprogress
						.GetImmediateRootElement()
						.Reload(globalprogress, UITableViewRowAnimation.Automatic);
				});
			};
			
			_sample = new DialogViewController (root);
			Navigation = new UINavigationController (_sample);
			Window.RootViewController = Navigation;
			Window.MakeKeyAndVisible ();

			var add = new UIBarButtonItem ("Add", 
				UIBarButtonItemStyle.Bordered, 
				 (sender, e) => {
					string url = string.Format(templateurl, 1);
				 Add(url);
			});
			
			var addall = new UIBarButtonItem ("All", 
				UIBarButtonItemStyle.Bordered, 
				 (sender, e) => {
					for(int i=1; i<80; i++) {
						string url = string.Format(templateurl, i);
					 Add(url);
					}
				});
			var zips = new UIBarButtonItem ("Zip", 
				UIBarButtonItemStyle.Bordered, 
				 (sender, e) => {
					foreach(string coll in collections) {
						string url = string.Format(zipurl, coll);
					 Add(url);
					}
				});


			var s404 = new UIBarButtonItem ("404", 
				UIBarButtonItemStyle.Bordered, 
				 (sender, e) => {
					string url = string.Format(httpurl, 404);
					 Add(url);
				});

			var s500 = new UIBarButtonItem ("500", 
				UIBarButtonItemStyle.Bordered, 
				 (sender, e) => {
					string url = string.Format(httpurl, 500);
					 Add(url);
				});

			var s301 = new UIBarButtonItem ("301", 
				UIBarButtonItemStyle.Bordered, 
				 (sender, e) => {
					string url = string.Format(httpurl, 301);
					 Add(url);
				});


			var s301p = new UIBarButtonItem ("301+", 
				UIBarButtonItemStyle.Bordered, 
				 (sender, e) => {
					string url = string.Format(redirecturl, 301);
					 Add(url);
				});

			var reset = new UIBarButtonItem ("Reset", 
				UIBarButtonItemStyle.Bordered, 
				async (sender, e) => {
					await _downloader.Reset();
					Sync();
				});
			
			var sync = new UIBarButtonItem ("Sync", 
				UIBarButtonItemStyle.Bordered, 
				(sender, e) => {
					Sync();
				});
			

			
			_sample.SetToolbarItems (new [] { add, addall, zips, s404, s500, s301, s301p, reset, sync }, true);
			Navigation.SetToolbarHidden (false, true);


			return true;
		}

		void Add (string url)
		{
			var s = string.Format ("{0}", 1);
			var element = new StringElement(s);
			_downloads.Insert(0, UITableViewRowAnimation.Top, element);

			_downloader.Queue (url, (download) => { 
				Console.WriteLine("[AppDelegate] ProgressChanged {0}", download.Id);
				InvokeOnMainThread(() => {
					element.Caption = Caption(download);
					element.GetImmediateRootElement()
						.Reload(element, UITableViewRowAnimation.Automatic);
				});
			});

		}

		string Caption(Download download) {
			float percent = (download.Written / (float)download.Total) * 100;
			int ipercent = (int)percent;
			string caption = string.Format("{0} {1} {2}% ({3} / {4})", 
				download.Id, 
				download.State.ToString(),
				ipercent,
				download.Written,
				download.Total
			);
			return caption;

		}

		void Sync ()
		{
			Console.WriteLine ("[AppDelegate] DidEnterBackground");

			var list = _downloader.List();
			var slist = list.Select (item => {
				var s = Caption(item);
				return s;
			});
			
			var elements = from s in slist
				select new StringElement(s);
			_downloads.Clear();
			_downloads.AddAll(elements);
		}
	}
}



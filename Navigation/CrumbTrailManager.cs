using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Routing;
using Navigation.Properties;

namespace Navigation
{
    internal static class CrumbTrailManager
    {
        private const string SEPARATOR = "!";
        private const string RET_1_SEP = "1" + SEPARATOR;
        private const string RET_2_SEP = "2" + SEPARATOR;
        private const string RET_3_SEP = "3" + SEPARATOR;
        private const string CRUMB_1_SEP = "4" + SEPARATOR;
        private const string CRUMB_2_SEP = "5" + SEPARATOR;

		internal static void BuildCrumbTrail()
        {
			string trail = StateContext.CrumbTrail;
			if (StateContext.PreviousStateKey != null)
            {
				bool initialState = StateContext.GetDialog(StateContext.StateKey).Initial == StateContext.GetState(StateContext.StateKey);
				if (initialState)
                {
                    trail = null;
                }
                else
                {
                    string croppedTrail = trail;
                    int crumbTrailSize = GetCrumbTrailSize(trail);
                    int count = 0;
                    bool repeatedState = false;
					if (StateContext.PreviousStateKey == StateContext.StateKey)
                    {
                        repeatedState = true;
                    }
                    while (!repeatedState && count < crumbTrailSize)
                    {
                        string trailState = GetCrumbTrailState(croppedTrail);
                        croppedTrail = CropCrumbTrail(croppedTrail);
						if (StateContext.GetState(trailState) == StateContext.GetState(StateContext.StateKey))
                        {
                            trail = croppedTrail;
                            repeatedState = true;
                        }
                        count++;
                    }

                    if (!repeatedState)
                    {
                        StringBuilder formattedReturnData = new StringBuilder();
						string prefix = string.Empty;
						if (StateContext.ReturnData != null)
                        {
							foreach (NavigationDataItem item in StateContext.ReturnData)
							{
								formattedReturnData.Append(prefix);
								formattedReturnData.Append(item.Key);
								formattedReturnData.Append(RET_1_SEP);
								formattedReturnData.Append(FormatURLObject(item.Value));
								prefix = RET_3_SEP;
							}
                        }
                        StringBuilder trailBuilder = new StringBuilder();
                        trailBuilder.Append(CRUMB_1_SEP);
						trailBuilder.Append(StateInfoConfig.GetStateKey(StateContext.GetState(StateContext.PreviousStateKey)));
                        trailBuilder.Append(CRUMB_2_SEP);
                        trailBuilder.Append(formattedReturnData.ToString());
                        trailBuilder.Append(trail);
                        trail = trailBuilder.ToString();
                    }
                }
            }
			StateContext.GenerateKey(trail);
        }

		private static string GetHref(string nextState, NavigationData navigationData, NavigationData returnData, string previousState, string crumbTrail, NavigationMode mode)
		{
			State state = StateContext.GetState(nextState);
			NameValueCollection coll = new NameValueCollection();
			coll[StateContext.STATE] = nextState;
			if (previousState != null && state.TrackCrumbTrail)
			{
				coll[StateContext.PREVIOUS_STATE] = previousState;
			}
			if (navigationData != null)
			{
				foreach (NavigationDataItem item in navigationData)
				{
					coll[item.Key] = FormatURLObject(item.Value);
				}
			}
			if (returnData != null && state.TrackCrumbTrail)
			{
				StringBuilder returnDataBuilder = new StringBuilder();
				string prefix = string.Empty;
				foreach (NavigationDataItem item in returnData)
				{
					returnDataBuilder.Append(prefix);
					returnDataBuilder.Append(item.Key);
					returnDataBuilder.Append(RET_1_SEP);
					returnDataBuilder.Append(FormatURLObject(item.Value));
					prefix = RET_3_SEP;
				}
				if (returnDataBuilder.Length > 0)
					coll[StateContext.RETURN_DATA] = returnDataBuilder.ToString();
			}
			if (crumbTrail != null && state.TrackCrumbTrail)
			{
				coll[StateContext.CRUMB_TRAIL] = crumbTrail;
			}
			coll = StateContext.ShieldEncode(coll, false);
			if (StateContext.GetState(nextState).Route.Length == 0 || mode == NavigationMode.Mock 
				|| RouteTable.Routes[nextState] == null)
			{
				StringBuilder href = new StringBuilder();
				href.Append(state.Page);
				href.Append("?");
				href.Append(HttpUtility.UrlEncode(StateContext.STATE));
				href.Append("=");
				href.Append(HttpUtility.UrlEncode(nextState));
				foreach (string key in coll)
				{
					if (key != StateContext.STATE)
					{
						href.Append("&");
						href.Append(HttpUtility.UrlEncode(key));
						href.Append("=");
						href.Append(HttpUtility.UrlEncode(coll[key]));
					}
				}
				return href.ToString();
			}
			else
			{
				RouteValueDictionary routeData = new RouteValueDictionary();
				foreach (string key in coll.Keys)
				{
					if (key != StateContext.STATE)
						routeData.Add(key, coll[key]);
				}
				return RouteTable.Routes.GetVirtualPath(null, nextState, routeData).VirtualPath.Insert(0, "~");
			}
		}

		private static string DecodeURLValue(string urlValue)
        {
            return urlValue.Replace("0" + SEPARATOR, SEPARATOR);
        }

		private static string EncodeURLValue(string urlValue)
        {
            return urlValue.Replace(SEPARATOR, "0" + SEPARATOR);
        }

		internal static string FormatURLObject(object urlObject)
        {
            string formattedValue;
			string urlObjectString = urlObject as string;
			if (urlObjectString != null)
            {
				formattedValue = EncodeURLValue(urlObjectString);
            }
            else
            {
                formattedValue = (string) ConverterFactory.GetConverterFromObj(urlObject).ConvertToInvariantString(urlObject);
                formattedValue = EncodeURLValue(formattedValue) + RET_2_SEP + ConverterFactory.GetKey(urlObject);
            }
            return formattedValue;
        }

		internal static object ParseURLString(string val)
		{
			object parsedVal;
			if (val.IndexOf(RET_2_SEP, StringComparison.Ordinal) > -1)
			{
				string[] arr = Regex.Split(val, RET_2_SEP);
				try
				{
					parsedVal = ConverterFactory.GetConverter(arr[1]).ConvertFromInvariantString(DecodeURLValue(arr[0]));
				}
				catch (Exception)
				{
					throw new UrlException(Resources.InvalidUrl);
				}
			}
			else
			{
				parsedVal = DecodeURLValue(val);
			}
			return parsedVal;
		}

		internal static List<Crumb> GetCrumbTrailHrefArray(NavigationMode mode)
        {
            List<Crumb> crumbTrailArray = new List<Crumb>();
            int arrayCount = 0;
			string crumbTrail = StateContext.CrumbTrail;
            int crumbTrailSize = GetCrumbTrailSize(crumbTrail);
            string href = null;
			NavigationData navigationData;
            while (arrayCount < crumbTrailSize)
            {
				string nextState = StateInfoConfig.GetDialogStateKey(StateContext.GetState(GetCrumbTrailState(crumbTrail)));
                navigationData = GetCrumbTrailData(crumbTrail);
                crumbTrail = CropCrumbTrail(crumbTrail);
				href = GetHref(nextState, navigationData, null, StateContext.StateKey, StateContext.CrumbTrailKey, mode);
				Crumb crumb = new Crumb(href, navigationData, StateContext.GetState(nextState));
                crumbTrailArray.Add(crumb);
                arrayCount++;
            }
            crumbTrailArray.Reverse();
            return crumbTrailArray;
        }

		private static int GetCrumbTrailSize(string trail)
        {
            int crumbTrailSize = trail == null ? 0 : Regex.Split(trail, CRUMB_1_SEP).Length - 1;
            return crumbTrailSize;
        }

		private static string CropCrumbTrail(string trail)
        {
            string croppedTrail;
            int nextTrailStart = trail.IndexOf(CRUMB_1_SEP, 1, StringComparison.Ordinal);
            if (nextTrailStart != -1)
            {
                croppedTrail = trail.Substring(nextTrailStart);
            }
            else
            {
                croppedTrail = "";
            }
            return croppedTrail;
        }

		private static string GetCrumbTrailState(string trail)
        {
            return Regex.Split(trail.Substring(CRUMB_1_SEP.Length), CRUMB_2_SEP)[0];
        }

		private static NavigationData GetCrumbTrailData(string trail)
        {
            NavigationData navData = null;
			string data = Regex.Split(trail.Substring(trail.IndexOf(CRUMB_2_SEP, StringComparison.Ordinal) + CRUMB_2_SEP.Length), CRUMB_1_SEP)[0];
            if (data.Length != 0)
            {
                navData = ParseReturnData(data);
            }
            return navData;
        }

		internal static string GetHref(string nextState, NavigationData navigationData, NavigationData returnData, NavigationMode mode)
        {
			return GetHref(nextState, navigationData, returnData, StateContext.StateKey, StateContext.CrumbTrailKey, mode);
        }

		internal static string GetRefreshHref(NavigationData refreshData, NavigationMode mode)
        {
			return GetHref(StateContext.StateKey, refreshData, null, StateContext.StateKey, StateContext.CrumbTrailKey, mode);
        }

		internal static object Parse(string key, string val)
        {
            object parsedVal;
			if (key == StateContext.RETURN_DATA)
            {
                parsedVal = ParseReturnData(val);
            }
            else
            {
				if (key == StateContext.CRUMB_TRAIL)
                {
                    parsedVal = val;
                }
                else
                {
					parsedVal = ParseURLString(val);
                }
            }
            return parsedVal;
        }

		private static NavigationData ParseReturnData(string returnData)
        {
            NavigationData navData = new NavigationData();
            string[] nameValuePair;
            string[] returnDataArray = Regex.Split(returnData, RET_3_SEP);
            for (int i = 0; i < returnDataArray.Length; i++)
            {
                nameValuePair = Regex.Split(returnDataArray[i], RET_1_SEP);
                navData.Add(nameValuePair[0], ParseURLString(nameValuePair[1]));
            }
            return navData;
        }
    }
}

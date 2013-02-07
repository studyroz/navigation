﻿using Navigation.Properties;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Compilation;
using System.Web.UI;

namespace Navigation
{
	/// <summary>
	/// Allows <see cref="System.Web.UI.Control"/> properties to be set from <see cref="Navigation.NavigationData"/>
	/// using just markup
	/// </summary>
	public class NavigationDataControlBuilderInterceptor : ControlBuilderInterceptor
	{
		private static Regex _NavigationDataBindingExpression = new Regex(@"^\{\s*(?<type>NavigationData|NavigationLink|NavigationBackLink|RefreshLink)(?<off>\*?)(\s+(?<key>[^\s]+.*)?)?\}$");

		/// <summary>
		/// Called before the <see cref="System.Web.UI.ControlBuilder"/> of an element in the markup is initialized
		/// </summary>
		/// <param name="controlBuilder">The control builder which is about to be initialized</param>
		/// <param name="parser">The <see cref="System.Web.UI.TemplateParser"/> which was used to parse the markup</param>
		/// <param name="parentBuilder">The parent control builder</param>
		/// <param name="type">The type of the control that this builder will create</param>
		/// <param name="tagName">The name of the tag to be built</param>
		/// <param name="id">The ID of the element in the markup</param>
		/// <param name="attributes">The list of attributes of the element in the markup</param>
		/// <param name="additionalState">The additional state which can be used to store and retrieve data within
		/// several methods of the <see cref="System.Web.Compilation.ControlBuilderInterceptor"/> class</param>
		public override void PreControlBuilderInit(ControlBuilder controlBuilder, TemplateParser parser, ControlBuilder parentBuilder, Type type, string tagName, string id, IDictionary attributes, IDictionary additionalState)
		{
			if (attributes != null)
			{
				Match navigationDataBindingMatch;
				Dictionary<string, Tuple<string, bool, NavigationDirection?>> navigationDataBindings = new Dictionary<string, Tuple<string, bool, NavigationDirection?>>();
				foreach (DictionaryEntry entry in attributes)
				{
					navigationDataBindingMatch = _NavigationDataBindingExpression.Match(((string)entry.Value).Trim());
					if (navigationDataBindingMatch.Success)
					{
						navigationDataBindings.Add((string)entry.Key, Tuple.Create(navigationDataBindingMatch.Groups["key"].Value.Trim(), navigationDataBindingMatch.Groups["off"].Value.Length != 0, GetNavigationDirection(navigationDataBindingMatch.Groups["type"].Value)));
					}
				}
				if (navigationDataBindings.Count > 0)
				{
					additionalState.Add("__NavigationData", navigationDataBindings);
					foreach (string key in navigationDataBindings.Keys)
						attributes.Remove(key);
				}
			}
		}

		private static NavigationDirection? GetNavigationDirection(string navigationDataBindingType)
		{
			if (StringComparer.InvariantCultureIgnoreCase.Compare(navigationDataBindingType, "NavigationLink") == 0)
				return NavigationDirection.Forward;
			if (StringComparer.InvariantCultureIgnoreCase.Compare(navigationDataBindingType, "NavigationBackLink") == 0)
				return NavigationDirection.Back;
			if (StringComparer.InvariantCultureIgnoreCase.Compare(navigationDataBindingType, "RefreshLink") == 0)
				return NavigationDirection.Refresh;
			return null;
		}

		/// <summary>
		/// Called after the <see cref="System.Web.UI.ControlBuilder"/> has completed generating code
		/// </summary>
		/// <param name="controlBuilder">The control builder instance</param>
		/// <param name="codeCompileUnit">A <see cref="System.CodeDom.CodeCompileUnit"/> object that is generated by the compilation</param>
		/// <param name="baseType">The type declaration of the code behind class or derived type</param>
		/// <param name="derivedType">The type declaration of top level markup element</param>
		/// <param name="buildMethod">The method with the necessary code to create the control and set the control's
		/// various properties, events, fields</param>
		/// <param name="dataBindingMethod">The method with code to evaluate data binding expressions within the control</param>
		/// <param name="additionalState">The additional state which can be used to store and retrieve data within
		/// several methods of the <see cref="System.Web.Compilation.ControlBuilderInterceptor"/> class</param>
		public override void OnProcessGeneratedCode(ControlBuilder controlBuilder, CodeCompileUnit codeCompileUnit, CodeTypeDeclaration baseType, CodeTypeDeclaration derivedType, CodeMemberMethod buildMethod, CodeMemberMethod dataBindingMethod, IDictionary additionalState)
		{
			if (buildMethod == null)
				return;
			Dictionary<string, Tuple<string, bool, NavigationDirection?>> navigationDataBindings = additionalState["__NavigationData"] as Dictionary<string, Tuple<string, bool, NavigationDirection?>>;
			if (navigationDataBindings == null)
				return;
			CodeLinePragma linePragma = null;
			foreach (CodeStatement statement in buildMethod.Statements)
			{
				if (statement.LinePragma != null)
					linePragma = statement.LinePragma;
			}
			CodeObjectCreateExpression navigationDataCreate = new CodeObjectCreateExpression(new CodeTypeReference("__NavigationData" + controlBuilder.ID), new CodeExpression[] { new CodeVariableReferenceExpression("__ctrl") });
			CodeVariableDeclarationStatement navigationDataVariable = new CodeVariableDeclarationStatement(new CodeTypeReference("__NavigationData" + controlBuilder.ID), "__navigationData", navigationDataCreate);
			navigationDataVariable.LinePragma = linePragma;
			buildMethod.Statements.Insert(buildMethod.Statements.Count - 1, navigationDataVariable);
			derivedType.Members.Add(BuildNavigationDataClass(controlBuilder, linePragma, navigationDataBindings, buildMethod));
		}

		private static CodeTypeDeclaration BuildNavigationDataClass(ControlBuilder controlBuilder, CodeLinePragma linePragma, Dictionary<string, Tuple<string, bool, NavigationDirection?>> navigationDataBindings, CodeMemberMethod buildMethod)
		{
			CodeTypeDeclaration navigationDataClass = new CodeTypeDeclaration("__NavigationData" + controlBuilder.ID);
			CodeAttributeDeclaration nonUserCodeAttribute = new CodeAttributeDeclaration(new CodeTypeReference(typeof(DebuggerNonUserCodeAttribute), CodeTypeReferenceOptions.GlobalReference));
			CodeConstructor constructor = new CodeConstructor();
			constructor.Attributes = MemberAttributes.Public;
			constructor.CustomAttributes.Add(nonUserCodeAttribute);
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(controlBuilder.ControlType, CodeTypeReferenceOptions.GlobalReference), "control"));
			constructor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_Control"), new CodeVariableReferenceExpression("control")));
			constructor.Statements[0].LinePragma = linePragma;
			navigationDataClass.Members.Add(constructor);
			CodeMemberField controlField = new CodeMemberField(new CodeTypeReference(controlBuilder.ControlType, CodeTypeReferenceOptions.GlobalReference), "_Control");
			navigationDataClass.Members.Add(controlField);
			CodePropertyReferenceExpression navigationData = new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(StateContext), CodeTypeReferenceOptions.GlobalReference)), "Data");
			CodeMemberMethod controlLoadListener = ConfigureListener("Control_Load", nonUserCodeAttribute);
			CodeMemberMethod pageLoadCompleteListener = ConfigureListener("Page_LoadComplete", nonUserCodeAttribute);
			CodeMemberMethod pagePreRenderCompleteListener = ConfigureListener("Page_PreRenderComplete", nonUserCodeAttribute);
			CodeMemberMethod pageSaveStateCompleteListener = ConfigureListener("Page_SaveStateComplete", nonUserCodeAttribute);
			foreach (KeyValuePair<string, Tuple<string, bool, NavigationDirection?>> tuple in navigationDataBindings)
			{
				if (!BuildNavigationDataEventListener(controlBuilder, tuple.Key, tuple.Value.Item1, tuple.Value.Item3, navigationData, navigationDataClass, nonUserCodeAttribute, linePragma, buildMethod))
					BuildNavigationDataStatements(controlBuilder, tuple.Key, tuple.Value.Item1, tuple.Value.Item3, navigationData, controlLoadListener, pageLoadCompleteListener, !tuple.Value.Item2 ? pagePreRenderCompleteListener : pageSaveStateCompleteListener, linePragma);
			}
			AttachEvent(false, controlLoadListener, "Load", typeof(EventHandler), linePragma, buildMethod, navigationDataClass);
			AttachEvent(true, pageLoadCompleteListener, "LoadComplete", typeof(EventHandler), linePragma, buildMethod, navigationDataClass);
			AttachEvent(true, pagePreRenderCompleteListener, "PreRenderComplete", typeof(EventHandler), linePragma, buildMethod, navigationDataClass);
			AttachEvent(true, pageSaveStateCompleteListener, "SaveStateComplete", typeof(EventHandler), linePragma, buildMethod, navigationDataClass);
			return navigationDataClass;
		}

		private static CodeMemberMethod ConfigureListener(string name, CodeAttributeDeclaration nonUserCodeAttribute)
		{
			CodeMemberMethod listener = new CodeMemberMethod();
			listener.Name = name;
			listener.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			listener.CustomAttributes.Add(nonUserCodeAttribute);
			listener.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object), CodeTypeReferenceOptions.GlobalReference), "sender"));
			listener.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(EventArgs), CodeTypeReferenceOptions.GlobalReference), "e"));
			return listener;
		}

		private static bool BuildNavigationDataEventListener(ControlBuilder controlBuilder, string key, string value, NavigationDirection? direction, CodePropertyReferenceExpression navigationData, CodeTypeDeclaration navigationDataClass, CodeAttributeDeclaration nonUserCodeAttribute, CodeLinePragma linePragma, CodeMemberMethod buildMethod)
		{
			if (direction.HasValue || !key.StartsWith("On", StringComparison.OrdinalIgnoreCase))
				return false;
			EventInfo eventInfo = controlBuilder.ControlType.GetEvent(key.Substring(2), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			if (eventInfo != null)
			{
				CodeMemberMethod listener = new CodeMemberMethod();
				listener.Name = "__ctrl_" + eventInfo.Name;
				listener.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				listener.CustomAttributes.Add(nonUserCodeAttribute);
				ParameterInfo[] parameters = eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters();
				foreach (ParameterInfo parameter in parameters)
				{
					listener.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(parameter.ParameterType, CodeTypeReferenceOptions.GlobalReference), parameter.Name));
				}
				listener.Statements.Add(new CodeAssignStatement(GetNavigationDataAsType(null, navigationData, value, null, controlBuilder, null, linePragma), GetNavigationDataAsType(typeof(bool), navigationData, "!" + value, null, controlBuilder, null, linePragma)));
				listener.Statements[0].LinePragma = linePragma;
				AttachEvent(false, listener, eventInfo.Name, eventInfo.EventHandlerType, linePragma, buildMethod, navigationDataClass);
				return true;
			}
			return false;
		}

		private static void BuildNavigationDataStatements(ControlBuilder controlBuilder, string key, string value, NavigationDirection? direction, CodePropertyReferenceExpression navigationData, CodeMemberMethod controlLoadListener, CodeMemberMethod pageLoadCompleteListener, CodeMemberMethod pagePreRenderCompleteListener, CodeLinePragma linePragma)
		{
			bool enabledOrVisible = StringComparer.InvariantCultureIgnoreCase.Compare(key, "Enabled") == 0 || StringComparer.InvariantCultureIgnoreCase.Compare(key, "Visible") == 0;
			CodeStatement controlNavigationDataAssign = GetNavigationDataAssign(controlBuilder, navigationData, key, value, direction, linePragma);
			if (controlNavigationDataAssign != null)
			{
				controlNavigationDataAssign.LinePragma = linePragma;
				if (enabledOrVisible)
				{
					controlLoadListener.Statements.Add(controlNavigationDataAssign);
					pageLoadCompleteListener.Statements.Add(controlNavigationDataAssign);
				}
				else
					pagePreRenderCompleteListener.Statements.Add(controlNavigationDataAssign);
			}
			else
			{
				if (controlBuilder.ControlType.GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public) != null)
					throw new HttpParseException(string.Format(CultureInfo.CurrentCulture, Resources.PropertyReadOnly, key), null, controlBuilder.PageVirtualPath, null, linePragma.LineNumber);
				throw new HttpParseException(string.Format(CultureInfo.CurrentCulture, Resources.PropertyMissing, controlBuilder.ControlType, key), null, controlBuilder.PageVirtualPath, null, linePragma.LineNumber);
			}
		}

		private static CodeStatement GetNavigationDataAssign(ControlBuilder controlBuilder, CodePropertyReferenceExpression navigationData, string key, string value, NavigationDirection? direction, CodeLinePragma linePragma)
		{
			PropertyInfo property = controlBuilder.ControlType.GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			if (property != null && property.CanWrite)
			{
				CodePropertyReferenceExpression controlProperty = new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_Control"), property.Name);
				return new CodeAssignStatement(controlProperty, GetNavigationDataAsType(property.PropertyType, navigationData, value, direction, controlBuilder, property.Name, linePragma));
			}
			FieldInfo field = controlBuilder.ControlType.GetField(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			if (field != null)
			{
				CodeFieldReferenceExpression controlField = new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_Control"), field.Name);
				return new CodeAssignStatement(controlField, GetNavigationDataAsType(field.FieldType, navigationData, value, direction, controlBuilder, field.Name, linePragma));
			}
			if (typeof(IAttributeAccessor).IsAssignableFrom(controlBuilder.ControlType))
			{
				CodeCastExpression attributeAccessor = new CodeCastExpression(new CodeTypeReference(typeof(IAttributeAccessor), CodeTypeReferenceOptions.GlobalReference), new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_Control"));
				CodeExpression[] setAttributeParams = new CodeExpression[] { new CodePrimitiveExpression(key), GetNavigationDataAsType(typeof(string), navigationData, value, direction, controlBuilder, "SetAttribute", linePragma) };
				return new CodeExpressionStatement(new CodeMethodInvokeExpression(attributeAccessor, "SetAttribute", setAttributeParams));
			}
			return null;
		}

		private static CodeExpression GetNavigationDataAsType(Type type, CodePropertyReferenceExpression navigationData, string key, NavigationDirection? direction, ControlBuilder controlBuilder, string name, CodeLinePragma linePragma)
		{
			if (direction.HasValue)
			{
				return GetLink(key, direction.Value, controlBuilder, linePragma);
			}
			if (type == typeof(NavigationData))
				return GetKeyAsNavigationData(key, controlBuilder.ControlType, name);
			int commaIndex = key.IndexOf(",", StringComparison.Ordinal);
			bool negation = key.StartsWith("!", StringComparison.Ordinal);
			string navigationDataKey = commaIndex <= 0 ? key : key.Substring(0, commaIndex).Trim();
			navigationDataKey = !negation ? navigationDataKey : navigationDataKey.Substring(1).Trim();
			CodeExpression navigationDataIndexer = new CodeIndexerExpression(navigationData, new CodePrimitiveExpression(navigationDataKey));
			if (negation)
				navigationDataIndexer = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeCastExpression(new CodeTypeReference(typeof(bool), CodeTypeReferenceOptions.GlobalReference), navigationDataIndexer), "Equals"), new CodePrimitiveExpression(false));
			if (type == null || (type == typeof(bool) && negation))
				return navigationDataIndexer;
			if (type == typeof(string))
			{
				CodePropertyReferenceExpression currentCulture = new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(CultureInfo), CodeTypeReferenceOptions.GlobalReference)), "CurrentCulture");
				if (commaIndex <= 0)
					return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(Convert), CodeTypeReferenceOptions.GlobalReference)), "ToString", new CodeExpression[] { navigationDataIndexer, currentCulture });
				else
					return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(string), CodeTypeReferenceOptions.GlobalReference)), "Format", new CodeExpression[] { currentCulture, new CodePrimitiveExpression(key.Substring(commaIndex + 1).Trim()), navigationDataIndexer });
			}
			else
			{
				return new CodeCastExpression(new CodeTypeReference(type, CodeTypeReferenceOptions.GlobalReference), navigationDataIndexer);
			}
		}

		private static CodeExpression GetLink(string key, NavigationDirection direction, ControlBuilder controlBuilder, CodeLinePragma linePragma)
		{
			int hashIndex = key.LastIndexOf("#", StringComparison.Ordinal);
			string link = hashIndex < 0 ? key : key.Substring(0, hashIndex);
			CodeExpression navigationLink = null;
			if (direction == NavigationDirection.Forward)
				navigationLink = GetNavigationLink(link, direction);
			if (direction == NavigationDirection.Back)
				navigationLink = GetNavigationBackLink(link, direction, controlBuilder, linePragma);
			if (direction == NavigationDirection.Refresh)
				navigationLink = GetRefreshLink(link, direction);
			if (hashIndex >= 0)
			{
				CodeMethodInvokeExpression concat = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(string), CodeTypeReferenceOptions.GlobalReference)), "Concat");
				concat.Parameters.Add(navigationLink);
				concat.Parameters.Add(new CodePrimitiveExpression("#"));
				concat.Parameters.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(HttpUtility), CodeTypeReferenceOptions.GlobalReference)), "UrlEncode", new CodePrimitiveExpression(key.Substring(hashIndex + 1))));
				return concat;
			}
			return navigationLink;
		}

		private static CodeExpression GetNavigationLink(string key, NavigationDirection direction)
		{
			CodeMethodInvokeExpression navigationLink = new CodeMethodInvokeExpression();
			navigationLink.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(StateController), CodeTypeReferenceOptions.GlobalReference)), "GetNavigationLink");
			int commaIndex = key.IndexOf(",", StringComparison.Ordinal);
			string action = commaIndex <= 0 ? key : key.Substring(0, commaIndex).Trim();
			navigationLink.Parameters.Add(new CodePrimitiveExpression(action));
			if (commaIndex > 0)
			{
				string data = key.Substring(commaIndex + 1).Trim();
				if (data.Length > 0)
				{
					CodeMethodInvokeExpression getNextState = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(StateController), CodeTypeReferenceOptions.GlobalReference)), "GetNextState");
					getNextState.Parameters.Add(new CodePrimitiveExpression(action));
					CodeMethodInvokeExpression parseNavigationData = new CodeMethodInvokeExpression();
					parseNavigationData.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(StateInfoConfig), CodeTypeReferenceOptions.GlobalReference)), "ParseNavigationDataExpression");
					parseNavigationData.Parameters.Add(new CodePrimitiveExpression(data));
					parseNavigationData.Parameters.Add(getNextState);
					navigationLink.Parameters.Add(parseNavigationData);
				}
			}
			return navigationLink;
		}

		private static CodeExpression GetNavigationBackLink(string key, NavigationDirection direction, ControlBuilder controlBuilder, CodeLinePragma linePragma)
		{
			CodeMethodInvokeExpression navigationLink = new CodeMethodInvokeExpression();
			navigationLink.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(StateController), CodeTypeReferenceOptions.GlobalReference)), "GetNavigationBackLink");
			try
			{
				navigationLink.Parameters.Add(new CodePrimitiveExpression(Convert.ToInt32(key, CultureInfo.CurrentCulture)));
			}
			catch (FormatException)
			{
				throw new HttpParseException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidDistanceString, key), null, controlBuilder.PageVirtualPath, null, linePragma.LineNumber);
			}
			catch (OverflowException)
			{
				throw new HttpParseException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidDistanceString, key), null, controlBuilder.PageVirtualPath, null, linePragma.LineNumber);
			}
			return navigationLink;
		}

		private static CodeExpression GetRefreshLink(string key, NavigationDirection direction)
		{
			CodeMethodInvokeExpression navigationLink = new CodeMethodInvokeExpression();
			navigationLink.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(StateController), CodeTypeReferenceOptions.GlobalReference)), "GetRefreshLink");
			if (key.Length != 0)
			{
				CodePropertyReferenceExpression state = new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(StateContext), CodeTypeReferenceOptions.GlobalReference)), "State");
				CodeMethodInvokeExpression parseNavigationData = new CodeMethodInvokeExpression();
				parseNavigationData.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(StateInfoConfig), CodeTypeReferenceOptions.GlobalReference)), "ParseNavigationDataExpression");
				parseNavigationData.Parameters.Add(new CodePrimitiveExpression(key));
				parseNavigationData.Parameters.Add(state);
				navigationLink.Parameters.Add(parseNavigationData);
			}
			else
			{
				navigationLink.Parameters.Add(new CodePrimitiveExpression(null));
			}
			return navigationLink;
		}

		private static CodeExpression GetKeyAsNavigationData(string key, Type controlType, string name)
		{
			CodeMethodInvokeExpression parseNavigationData = new CodeMethodInvokeExpression();
			parseNavigationData.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(StateInfoConfig), CodeTypeReferenceOptions.GlobalReference)), "ParseNavigationDataExpression");
			parseNavigationData.Parameters.Add(new CodePrimitiveExpression(key));
			if (typeof(NavigationHyperLink).IsAssignableFrom(controlType) && StringComparer.InvariantCultureIgnoreCase.Compare(name, "ToData") == 0)
				parseNavigationData.Parameters.Add(new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_Control"), "NextState"));
			else
				parseNavigationData.Parameters.Add(new CodePrimitiveExpression(null));
			return parseNavigationData;
		}

		private static void AttachEvent(bool page, CodeMemberMethod listener, string name, Type eventHandlerType, CodeLinePragma linePragma, CodeMemberMethod buildMethod, CodeTypeDeclaration navigationDataClass)
		{
			if (listener.Statements.Count > 0)
			{
				navigationDataClass.Members.Add(listener);
				CodeDelegateCreateExpression navigationDataDelegate = new CodeDelegateCreateExpression(new CodeTypeReference(eventHandlerType, CodeTypeReferenceOptions.GlobalReference), new CodeVariableReferenceExpression("__navigationData"), listener.Name);
				CodeExpression expression;
				if (page)
					expression = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "Page");
				else
					expression = new CodeVariableReferenceExpression("__ctrl");
				CodeAttachEventStatement pageAttachEvent = new CodeAttachEventStatement(expression, name, navigationDataDelegate);
				pageAttachEvent.LinePragma = linePragma;
				buildMethod.Statements.Insert(buildMethod.Statements.Count - 1, pageAttachEvent);
			}
		}
	}
}

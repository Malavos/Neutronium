﻿using FluentAssertions;
using Neutronium.Core;
using Neutronium.Core.WebBrowserEngine.JavascriptObject;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MoreCollection.Extensions;
using Neutronium.Core.Test.Helper;
using Tests.Infra.IntegratedContextTesterHelper.Windowless;
using Tests.Universal.HTMLBindingTests;
using Tests.Universal.HTMLBindingTests.Helper;
using Xunit;
using Xunit.Abstractions;

namespace VueFramework.Test.IntegratedInfra
{
    public abstract class HtmlVueBindingTests : HtmlBindingTests
    {
        protected HtmlVueBindingTests(IWindowLessHTMLEngineProvider context, ITestOutputHelper output)
            : base(context, output)
        {
        }

        private void CheckReadOnly(IJavascriptObject javascriptObject, bool isReadOnly)
        {
            var readOnly = GetBoolAttribute(javascriptObject, NeutroniumConstants.ReadOnlyFlag);
            readOnly.Should().Be(isReadOnly);

            CheckHasListener(javascriptObject, !isReadOnly);
        }

        private void CheckHasListener(IJavascriptObject javascriptObject, bool hasListener)
        {
            var silenterRoot = GetAttribute(javascriptObject, "__silenter");

            if (hasListener)
            {
                silenterRoot.IsObject.Should().BeTrue();
            }
            else
            {
                silenterRoot.IsUndefined.Should().BeTrue();
            }
        }

        public static IEnumerable<object> ReadWriteTestData
        {
            get
            {
                yield return new object[] { typeof(ReadOnlyTestViewModel), true };
                yield return new object[] { typeof(ReadWriteTestViewModel), false };
            }
        }

        [Theory]
        [MemberData(nameof(ReadWriteTestData))]
        public async Task TwoWay_should_create_listener_only_for_write_property(Type type, bool readOnly)
        {
            var datacontext = Activator.CreateInstance(type);

            var test = new TestInContext()
            {
                Bind = (win) => Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = (mb) =>
                {
                    var js = mb.JsRootObject;
                    CheckReadOnly(js, readOnly);
                }
            };

            await RunAsync(test);
        }

        [Theory]
        [MemberData(nameof(ReadWriteTestData))]
        public async Task TwoWay_should_update_from_csharp_readonly_property(Type type, bool readOnly)
        {
            var datacontext = Activator.CreateInstance(type) as ReadOnlyTestViewModel;

            var test = new TestInContextAsync()
            {
                Bind = (win) => Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;
                    var newValue = 55;
                    DoSafeUI(() => datacontext.SetReadOnly(newValue));

                    await Task.Delay(150);
                    var readOnlyValue = GetIntAttribute(js, "ReadOnly");

                    readOnlyValue.Should().Be(newValue);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_should_update_from_csharp_readwrite_property()
        {
            var datacontext = new ReadWriteTestViewModel();

            var test = new TestInContextAsync()
            {
                Bind = (win) => Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;
                    var newValue = 550;
                    DoSafeUI(() => datacontext.ReadWrite = newValue);

                    await Task.Delay(150);
                    var readOnlyValue = GetIntAttribute(js, "ReadWrite");

                    readOnlyValue.Should().Be(newValue);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_should_update_from_js_readwrite_property()
        {
            var datacontext = new ReadWriteTestViewModel();

            var test = new TestInContextAsync()
            {
                Bind = (win) => Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;
                    var newValue = 1200;
                    var jsValue = _WebView.Factory.CreateInt(newValue);
                    SetAttribute(js, "ReadWrite", jsValue);
                    await Task.Delay(150);

                    DoSafeUI(() => datacontext.ReadWrite.Should().Be(newValue));
                }
            };

            await RunAsync(test);
        }

        [Theory]
        [MemberData(nameof(BasicVmData))]
        public async Task TwoWay_should_clean_javascriptObject_listeners_when_object_is_not_part_of_the_graph(BasicVm remplacementChild)
        {
            var datacontext = new BasicFatherVm();
            var child = new BasicVm();
            datacontext.Child = child;

            var test = new TestInContextAsync()
            {
                Bind = (win) => Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;
                    var childJs = GetAttribute(js, "Child");

                    CheckReadOnly(childJs, false);

                    DoSafeUI(() => datacontext.Child = remplacementChild);
                    await Task.Delay(150);

                    CheckHasListener(childJs, false);
                }
            };

            await RunAsync(test);
        }

        [Fact]
        public async Task TwoWay_should_clean_javascriptObject_listeners_when_object_is_not_part_of_the_graph_js()
        {
            var datacontext = new BasicFatherVm();
            var child = new BasicVm();
            datacontext.Child = child;

            var test = new TestInContextAsync()
            {
                Bind = (win) => Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;
                    var childJs = GetAttribute(js, "Child");

                    CheckReadOnly(childJs, false);

                    var nullJs = Factory.CreateNull();
                    SetAttribute(js, "Child", nullJs);

                    await Task.Delay(150);

                    DoSafeUI(() => datacontext.Child.Should().BeNull());

                    child.ListenerCount.Should().Be(0);

                    await Task.Delay(100);

                    CheckHasListener(childJs, false);
                }
            };

            await RunAsync(test);
        }

        [Theory]
        [MemberData(nameof(BasicVmData))]
        public async Task TwoWay_should_clean_javascriptObject_listeners_when_object_is_not_part_of_the_graph_array_context(BasicVm remplacementChild)
        {
            var datacontext = new BasicListVm();
            var child = new BasicVm();
            datacontext.Children.Add(child);

            var test = new TestInContextAsync()
            {
                Bind = (win) => Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    CheckReadOnly(js, true);

                    var childrenJs = GetCollectionAttribute(js, "Children");
                    var childJs = childrenJs.GetValue(0);

                    CheckReadOnly(childJs, false);

                    DoSafeUI(() => datacontext.Children[0] = remplacementChild);
                    await Task.Delay(150);

                    CheckHasListener(childJs, false);
                }
            };

            await RunAsync(test);
        }


        [Fact]
        public async Task TwoWay_should_clean_javascriptObject_listeners_when_object_is_not_part_of_the_graph_array_js_context()
        {
            var datacontext = new BasicListVm();
            var child = new BasicVm();
            datacontext.Children.Add(child);

            var test = new TestInContextAsync()
            {
                Bind = (win) => Bind(win, datacontext, JavascriptBindingMode.TwoWay),
                Test = async (mb) =>
                {
                    var js = mb.JsRootObject;

                    CheckReadOnly(js, true);

                    var childrenJs = GetCollectionAttribute(js, "Children");
                    var childJs = childrenJs.GetValue(0);

                    CheckReadOnly(childJs, false);

                    Call(childrenJs, "pop");

                    await Task.Delay(150);

                    CheckHasListener(childJs, false);
                }
            };

            await RunAsync(test);
        }
    }
}
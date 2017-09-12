﻿using System.Collections.Specialized;
using System.ComponentModel;
using Neutronium.Core.Binding.GlueObject;

namespace Neutronium.Core.Binding.Listeners
{
    public interface IObjectChangesListener
    {
        void OnObject(INotifyPropertyChanged iobject);

        void OnCollection(INotifyCollectionChanged icollection);

        void OnCommand(JsCommand icommand);
    }
}
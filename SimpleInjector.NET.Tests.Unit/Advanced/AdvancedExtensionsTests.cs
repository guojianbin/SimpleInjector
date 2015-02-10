﻿#pragma warning disable 618
namespace SimpleInjector.Tests.Unit.Advanced
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using SimpleInjector.Advanced;
    using SimpleInjector.Tests.Unit.Extensions;

    [TestClass]
    public class AdvancedExtensionsTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IsLocked_WithNullArgument_ThrowsException()
        {
            // Act
            AdvancedExtensions.IsLocked(null);
        }

        [TestMethod]
        public void GetInitializer_NoInitializerRegisteredForRequestedType_ReturnsNull()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            var initializer = AdvancedExtensions.GetInitializer<IDisposable>(container);

            // Assert
            Assert.IsNull(initializer);
        }

        [TestMethod]
        public void GetInitializer_InitializerRegisteredForRequestedType_ReturnsADelegate()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterInitializer<IDisposable>(d => { });

            // Act
            var initializer = AdvancedExtensions.GetInitializer<IDisposable>(container);

            // Assert
            Assert.IsNotNull(initializer);
        }

        [TestMethod]
        public void GetInitializer_CallingTheReturnedDelegate_CallsTheRegisteredDelegate()
        {
            // Arrange
            bool called = false;

            var container = ContainerFactory.New();

            container.RegisterInitializer<IDisposable>(d => { called = true; });

            // Act
            var initializer = AdvancedExtensions.GetInitializer<IDisposable>(container);

            initializer(null);

            // Assert
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void GetInitializer_CallingTheReturnedDelegateWithTwoDelegatesRegistered_CallsTheRegisteredDelegates()
        {
            // Arrange
            bool called1 = false;
            bool called2 = false;

            var container = ContainerFactory.New();

            container.RegisterInitializer<IDisposable>(d => { called1 = true; });
            container.RegisterInitializer<IDisposable>(d => { called2 = true; });

            // Act
            var initializer = AdvancedExtensions.GetInitializer<IDisposable>(container);

            initializer(null);

            // Assert
            Assert.IsTrue(called1);
            Assert.IsTrue(called2);
        }

        [TestMethod]
        public void GetInitializer_CallingTheReturnedDelegate_CallsTheDelegateWithTheExpectedInstance()
        {
            // Arrange
            object actualInstance = null;

            var container = ContainerFactory.New();

            container.RegisterInitializer<object>(d => { actualInstance = d; });

            // Act
            var initializer = AdvancedExtensions.GetInitializer<object>(container);

            object expectedInstance = new object();

            initializer(expectedInstance);

            // Assert
            Assert.IsTrue(object.ReferenceEquals(expectedInstance, actualInstance));
        }

        [TestMethod]
        public void AppendToCollection_WithValidArguments_Suceeds()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            container.AppendToCollection(typeof(object), CreateRegistration(container));
        }

        [TestMethod]
        public void AppendToCollection_WithNullContainerArgument_ThrowsException()
        {
            // Arrange
            Container invalidContainer = null;

            // Act
            Action action =
                () => invalidContainer.AppendToCollection(typeof(object), CreateRegistration(new Container()));

            // Assert
            AssertThat.ThrowsWithParamName<ArgumentNullException>("container", action);
        }

        [TestMethod]
        public void AppendToCollection_WithNullServiceTypeArgument_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();

            Type invalidServiceType = null;

            // Act
            Action action =
                () => container.AppendToCollection(invalidServiceType, CreateRegistration(container));

            // Assert
            AssertThat.ThrowsWithParamName<ArgumentNullException>("serviceType", action);
        }

        [TestMethod]
        public void AppendToCollection_WithNullRegistrationArgument_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();

            Registration invalidRegistration = null;

            // Act
            Action action = () => container.AppendToCollection(typeof(object), invalidRegistration);

            // Assert
            AssertThat.ThrowsWithParamName<ArgumentNullException>("registration", action);
        }

        [TestMethod]
        public void AppendToCollection_WithRegistrationForDifferentContainer_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();

            var differentContainer = new Container();

            Registration invalidRegistration = CreateRegistration(differentContainer);

            // Act
            Action action = () => container.AppendToCollection(typeof(object), invalidRegistration);

            // Assert
            AssertThat.ThrowsWithParamName<ArgumentException>("registration", action);
            AssertThat.ThrowsWithExceptionMessageContains<ArgumentException>(
                "The supplied Registration belongs to a different container.", action);
        }

        [TestMethod]
        public void AppendToCollection_ForUnregisteredCollection_ResolvesThatRegistrationWhenRequested()
        {
            // Arrange
            var container = ContainerFactory.New();

            var registration = Lifestyle.Transient.CreateRegistration<IPlugin, PluginImpl>(container);

            container.AppendToCollection(typeof(IPlugin), registration);

            // Act
            var instance = container.GetAllInstances<IPlugin>().Single();

            // Assert
            Assert.IsInstanceOfType(instance, typeof(PluginImpl));
        }

        [TestMethod]
        public void AppendToCollection_CalledTwice_ResolvesBothRegistrationsWhenRequested()
        {
            // Arrange
            var container = ContainerFactory.New();

            var registration1 = Lifestyle.Transient.CreateRegistration<IPlugin, PluginImpl>(container);
            var registration2 = Lifestyle.Transient.CreateRegistration<IPlugin, PluginImpl2>(container);

            container.AppendToCollection(typeof(IPlugin), registration1);
            container.AppendToCollection(typeof(IPlugin), registration2);

            // Act
            var instances = container.GetAllInstances<IPlugin>().ToArray();

            // Assert
            Assert.IsInstanceOfType(instances[0], typeof(PluginImpl));
            Assert.IsInstanceOfType(instances[1], typeof(PluginImpl2));
        }

        [TestMethod]
        public void AppendToCollection_CalledAfterRegisterAllWithTypes_CombinedAllRegistrationsWhenRequested()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterAll<IPlugin>(typeof(PluginImpl));

            var registration = Lifestyle.Transient.CreateRegistration<IPlugin, PluginImpl2>(container);

            container.AppendToCollection(typeof(IPlugin), registration);

            // Act
            var instances = container.GetAllInstances<IPlugin>().ToArray();

            // Assert
            Assert.IsInstanceOfType(instances[0], typeof(PluginImpl));
            Assert.IsInstanceOfType(instances[1], typeof(PluginImpl2));
        }

        [TestMethod]
        public void AppendToCollection_CalledAfterRegisterAllWithRegistration_CombinedAllRegistrationsWhenRequested()
        {
            // Arrange
            var container = ContainerFactory.New();

            var registration1 = Lifestyle.Transient.CreateRegistration<IPlugin, PluginImpl>(container);
            var registration2 = Lifestyle.Transient.CreateRegistration<IPlugin, PluginImpl2>(container);

            container.RegisterAll(typeof(IPlugin), new[] { registration1 });

            container.AppendToCollection(typeof(IPlugin), registration2);

            // Act
            var instances = container.GetAllInstances<IPlugin>().ToArray();

            // Assert
            Assert.IsInstanceOfType(instances[0], typeof(PluginImpl));
            Assert.IsInstanceOfType(instances[1], typeof(PluginImpl2));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AppendToCollection_CalledAfterTheFirstItemIsRequested_ThrowsExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            var registration1 = Lifestyle.Transient.CreateRegistration<IPlugin, PluginImpl>(container);
            var registration2 = Lifestyle.Transient.CreateRegistration<IPlugin, PluginImpl2>(container);

            container.AppendToCollection(typeof(IPlugin), registration1);

            var instances = container.GetAllInstances<IPlugin>().ToArray();

            // Act
            container.AppendToCollection(typeof(IPlugin), registration2);
        }

        [TestMethod]
        public void AppendToCollection_OnContainerUncontrolledCollection_ThrowsExpressiveException()
        {
            // Arrange
            var container = ContainerFactory.New();

            IEnumerable<IPlugin> containerUncontrolledCollection = new[] { new PluginImpl() };

            container.RegisterAll<IPlugin>(containerUncontrolledCollection);

            var registration = Lifestyle.Transient.CreateRegistration<IPlugin, PluginImpl>(container);

            // Act
            Action action = () => container.AppendToCollection(typeof(IPlugin), registration);

            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<NotSupportedException>(@"
                appending registrations to these collections is not supported. Please register the collection
                with one of the other RegisterAll overloads is appending is required."
                .TrimInside(),
                action);
        }

        [TestMethod]
        public void GetAllInstances_RegistrationAppendedToExistingOpenGenericRegistration_ResolvesTheExtectedCollection()
        {
            // Arrange
            Type[] expectedHandlerTypes = new[]
            {
                typeof(NewConstraintEventHandler<StructEvent>),
                typeof(StructEventHandler),
            };

            var container = ContainerFactory.New();

            container.RegisterAll(typeof(IEventHandler<>), new[] { typeof(NewConstraintEventHandler<>) });

            var registration = Lifestyle.Transient.CreateRegistration<StructEventHandler>(container);

            container.AppendToCollection(typeof(IEventHandler<>), registration);

            // Act
            Type[] actualHandlerTypes = container.GetAllInstances(typeof(IEventHandler<StructEvent>))
                .Select(h => h.GetType()).ToArray();

            // Assert
            Assert.AreEqual(
                expected: expectedHandlerTypes.ToFriendlyNamesText(),
                actual: actualHandlerTypes.ToFriendlyNamesText());
        }
        
        [TestMethod]
        public void GetAllInstances_RegistrationPrependedToExistingOpenGenericRegistration_ResolvesTheExtectedCollection()
        {
            // Arrange
            Type[] expectedHandlerTypes = new[]
            {
                typeof(StructEventHandler),
                typeof(NewConstraintEventHandler<StructEvent>),
            };

            var container = ContainerFactory.New();

            var registration = Lifestyle.Transient.CreateRegistration<StructEventHandler>(container);

            container.AppendToCollection(typeof(IEventHandler<>), registration);

            container.RegisterAll(typeof(IEventHandler<>), new[] { typeof(NewConstraintEventHandler<>) });

            // Act
            Type[] actualHandlerTypes = container.GetAllInstances(typeof(IEventHandler<StructEvent>))
                .Select(h => h.GetType()).ToArray();

            // Assert
            Assert.AreEqual(
                expected: expectedHandlerTypes.ToFriendlyNamesText(),
                actual: actualHandlerTypes.ToFriendlyNamesText());
        }

        [TestMethod]
        public void GetAllInstances_MultipleAppendedOpenGenericTypes_ResolvesTheExpectedCollection()
        {
            // Arrange
            Type[] expectedHandlerTypes = new[]
            {
                typeof(NewConstraintEventHandler<StructEvent>),
                typeof(StructConstraintEventHandler<StructEvent>),
                typeof(AuditableEventEventHandler<StructEvent>)
            };

            var container = ContainerFactory.New();

            container.AppendToCollection(typeof(IEventHandler<>), typeof(NewConstraintEventHandler<>));
            container.AppendToCollection(typeof(IEventHandler<>), typeof(StructConstraintEventHandler<>));
            container.AppendToCollection(typeof(IEventHandler<>), typeof(AuditableEventEventHandler<>));

            // Act
            Type[] actualHandlerTypes = container.GetAllInstances(typeof(IEventHandler<StructEvent>))
                .Select(h => h.GetType()).ToArray();

            // Assert
            Assert.AreEqual(
                expected: expectedHandlerTypes.ToFriendlyNamesText(),
                actual: actualHandlerTypes.ToFriendlyNamesText());
        }
        
        [TestMethod]
        public void GetAllInstances_MultipleAppendedOpenGenericTypesMixedWithClosedGenericRegisterAll_ResolvesTheExpectedCollection()
        {
            // Arrange
            Type[] expectedHandlerTypes = new[]
            {
                typeof(NewConstraintEventHandler<StructEvent>),
                typeof(AuditableEventEventHandler<StructEvent>),
                typeof(StructConstraintEventHandler<StructEvent>),
            };

            var container = ContainerFactory.New();

            container.AppendToCollection(typeof(IEventHandler<>), typeof(NewConstraintEventHandler<>));

            container.RegisterAll(typeof(IEventHandler<StructEvent>), new[] 
            { 
                typeof(AuditableEventEventHandler<StructEvent>) 
            });

            container.AppendToCollection(typeof(IEventHandler<>), typeof(StructConstraintEventHandler<>));

            // Act
            Type[] actualHandlerTypes = container.GetAllInstances(typeof(IEventHandler<StructEvent>))
                .Select(h => h.GetType()).ToArray();

            // Assert
            Assert.AreEqual(
                expected: expectedHandlerTypes.ToFriendlyNamesText(),
                actual: actualHandlerTypes.ToFriendlyNamesText());
        }

        [TestMethod]
        public void GetAllInstances_MultipleOpenGenericTypesAppendedToPreRegistrationWithOpenGenericType_ResolvesTheExpectedCollection()
        {
            // Arrange
            Type[] expectedHandlerTypes = new[]
            {
                typeof(NewConstraintEventHandler<StructEvent>),
                typeof(StructConstraintEventHandler<StructEvent>),
                typeof(AuditableEventEventHandler<StructEvent>)
            };

            var container = ContainerFactory.New();

            container.RegisterAll(typeof(IEventHandler<>), new[] { typeof(NewConstraintEventHandler<>) });

            container.AppendToCollection(typeof(IEventHandler<>), typeof(StructConstraintEventHandler<>));
            container.AppendToCollection(typeof(IEventHandler<>), typeof(AuditableEventEventHandler<>));

            // Act
            Type[] actualHandlerTypes = container.GetAllInstances(typeof(IEventHandler<StructEvent>))
                .Select(h => h.GetType()).ToArray();

            // Assert
            Assert.AreEqual(
                expected: expectedHandlerTypes.ToFriendlyNamesText(),
                actual: actualHandlerTypes.ToFriendlyNamesText());
        }

        [TestMethod]
        public void GetAllInstances_RegistrationAppendedToExistingRegistrationForSameClosedType_ResolvesTheInstanceWithExpectedLifestyle()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterAll(typeof(IEventHandler<>), new[] 
            { 
                // Here we make a closed registration; this causes an explicit registration for the
                // IEventHandlerStructEvent> collection.
                typeof(NewConstraintEventHandler<StructEvent>),
            });

            var registration = Lifestyle.Singleton
                .CreateRegistration(typeof(StructConstraintEventHandler<StructEvent>), container);

            container.AppendToCollection(typeof(IEventHandler<>), registration);

            // Act
            var handler1 = container.GetAllInstances<IEventHandler<StructEvent>>().Last();
            var handler2 = container.GetAllInstances<IEventHandler<StructEvent>>().Last();

            // Assert
            Assert.IsInstanceOfType(handler1, typeof(StructConstraintEventHandler<StructEvent>));
            Assert.AreSame(handler1, handler2, "The instance was expected to be registered as singleton");
        }
        
        [TestMethod]
        public void GetAllInstances_DelegatedRegistrationAppendedToExistingRegistrationForSameClosedType_ResolvesTheInstanceWithExpectedLifestyle()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterAll(typeof(IEventHandler<>), new[] 
            { 
                typeof(NewConstraintEventHandler<StructEvent>),
            });

            var registration = Lifestyle.Singleton.CreateRegistration(
                typeof(IEventHandler<StructEvent>),
                () => new StructConstraintEventHandler<StructEvent>(),
                container);

            container.AppendToCollection(typeof(IEventHandler<>), registration);

            // Act
            var handler1 = container.GetAllInstances<IEventHandler<StructEvent>>().Last();
            var handler2 = container.GetAllInstances<IEventHandler<StructEvent>>().Last();

            // Assert
            Assert.IsInstanceOfType(handler1, typeof(StructConstraintEventHandler<StructEvent>));
            Assert.AreSame(handler1, handler2, "The instance was expected to be registered as singleton");
        }

        [TestMethod]
        public void GetItem_NoValueSet_ReturnsNull()
        {
            // Arrange
            object key = new object();

            var container = ContainerFactory.New();

            // Act
            object item = container.GetItem(key);

            // Assert
            Assert.IsNull(item);
        }

        [TestMethod]
        public void GetItem_WithValueSet_ReturnsThatItem()
        {
            // Arrange
            object key = new object();
            object expectedItem = new object();

            var container = ContainerFactory.New();

            container.SetItem(key, expectedItem);

            // Act
            object actualItem = container.GetItem(key);

            // Assert
            Assert.AreSame(expectedItem, actualItem);
        }

        [TestMethod]
        public void GetItem_WithValueSetInOneContainer_DoesNotReturnThatItemInAnotherContainer()
        {
            // Arrange
            object key = new object();
            object expectedItem = new object();

            var container1 = ContainerFactory.New();
            var container2 = ContainerFactory.New();

            container1.SetItem(key, expectedItem);

            // Act
            object actualItem = container2.GetItem(key);

            // Assert
            Assert.IsNull(actualItem, "The items dictionary is expected to be container bound. Not static!");
        }

        [TestMethod]
        public void GetItem_WithValueSetTwice_ReturnsLastItem()
        {
            // Arrange
            object key = new object();
            object firstItem = new object();
            object expectedItem = new object();

            var container = ContainerFactory.New();

            container.SetItem(key, firstItem);
            container.SetItem(key, expectedItem);

            // Act
            object actualItem = container.GetItem(key);

            // Assert
            Assert.AreSame(expectedItem, actualItem);
        }

        [TestMethod]
        public void GetItem_WithValueReset_ReturnsNull()
        {
            // Arrange
            object key = new object();

            var container = ContainerFactory.New();

            container.SetItem(key, new object());
            container.SetItem(key, null);

            // Act
            object item = container.GetItem(key);

            // Assert
            // This test looks odd, but under the cover the item is removed from the collection when null
            // is supplied to prevent the dictionary from ever increasing, but we have to test this code path.
            Assert.IsNull(item, "When a value is overridden with null, it is expected to return null.");
        }

        [TestMethod]
        public void GetItem_WithNullKey_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            Action action = () => container.GetItem(null);

            // Assert
            AssertThat.Throws<ArgumentNullException>(action);
        }

        [TestMethod]
        public void SetItem_WithNullKey_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            Action action = () => container.SetItem(null, new object());

            // Assert
            AssertThat.Throws<ArgumentNullException>(action);
        }

        private static Registration CreateRegistration(Container container)
        {
            return Lifestyle.Transient.CreateRegistration<IPlugin, PluginImpl>(container);
        }
    }
}
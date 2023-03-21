##AutoCompleteMessages = false

You're correct that the PeekLock mode is the default for Service Bus queues and subscriptions, which means that messages are locked for processing and removed from the queue only after they have been completed.

In this case, setting AutoCompleteMessages to false is not strictly necessary, since the default behavior is already what we want. However, it can be a good practice to explicitly set this property to false in case the default behavior changes in the future, or to make it clear to anyone reading the code that we intend to manually complete the messages.

So, while setting AutoCompleteMessages to false is not strictly required, it can be a helpful practice to ensure the behavior we want and make the code more clear.
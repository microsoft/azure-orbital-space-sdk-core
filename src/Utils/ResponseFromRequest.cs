


using Microsoft.Azure.SpaceFx.MessageFormats.Common;

namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    public partial class Utils {
        /// <summary>
        /// Copies the 'TrackingId' from the request header to the response header.
        /// </summary>
        /// <typeparam name="T1">The type of the request object. Must be a type that implements IMessage.</typeparam>
        /// <typeparam name="T2">The type of the response object. Must be a type that implements IMessage.</typeparam>
        /// <param name="requestObject">The request object that contains the 'RequestHeader' field.</param>
        /// <param name="responseObject">The response object that contains the 'ResponseHeader' field.</param>
        /// <exception cref="InvalidOperationException">Thrown when either the request or response object does not contain the 'RequestHeader' or 'ResponseHeader' field.</exception>

        public static T2 ResponseFromRequest<T1, T2>(T1 requestObject, T2 responseObject)
            where T1 : IMessage
            where T2 : IMessage {
            MessageDescriptor requestDescriptor = requestObject.Descriptor;
            MessageDescriptor responseDescriptor = responseObject.Descriptor;

            FieldDescriptor requestHeaderField = requestDescriptor.FindFieldByName("requestHeader");
            FieldDescriptor responseHeaderField = responseDescriptor.FindFieldByName("responseHeader");
            object trackingId = Guid.NewGuid().ToString(), correlationId;


            if (requestHeaderField == null || responseHeaderField == null) {
                throw new InvalidOperationException("Request and Response must have RequestHeader and ResponseHeader fields");
            }

            IMessage requestHeader = (IMessage) requestHeaderField.Accessor.GetValue(requestObject);
            IMessage responseHeader = (IMessage) responseHeaderField.Accessor.GetValue(responseObject);

            if (responseHeader == null) {
                System.Type responseType = responseHeaderField.MessageType.ClrType;
                responseHeader = Activator.CreateInstance(responseType) as IMessage ?? throw new InvalidOperationException("Failed to create response header");
                responseHeaderField.Accessor.SetValue(responseObject, responseHeader);
            }

            FieldDescriptor trackingIdField = requestHeader.Descriptor.FindFieldByName("trackingId");
            FieldDescriptor trackingIdFieldResponse = responseHeader.Descriptor.FindFieldByName("trackingId");

            if (trackingIdField != null) {
                trackingId = trackingIdField.Accessor.GetValue(requestHeader);
                if (trackingId == null || trackingId.ToString() == Guid.Empty.ToString()) {
                    trackingId = Guid.NewGuid().ToString();
                }

                trackingIdFieldResponse.Accessor.SetValue(responseHeader, trackingId);
            }

            FieldDescriptor correlationIdField = requestHeader.Descriptor.FindFieldByName("correlationId");
            FieldDescriptor correlationIdFieldResponse = responseHeader.Descriptor.FindFieldByName("correlationId");

            if (correlationIdField != null) {
                correlationId = correlationIdField.Accessor.GetValue(requestHeader);
                if (correlationId == null || correlationId.ToString() == Guid.Empty.ToString()) {
                    correlationId = trackingId;
                }
                correlationIdFieldResponse.Accessor.SetValue(responseHeader, correlationId);
            }

            FieldDescriptor statusFieldResponse = responseHeader.Descriptor.FindFieldByName("status");
            statusFieldResponse.Accessor.SetValue(responseHeader, Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Unknown);

            return responseObject;
        }
    }
}

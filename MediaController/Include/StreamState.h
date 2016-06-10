#ifndef StreamState_h__
#define StreamState_h__

#include "Controller.h"

namespace MediaController {

    /// <summary>
    /// Provides an interface for managing calls to the stream based on its current state.
    /// </summary>
    class StreamState {
    public:

        /// <summary>
        /// Virtual destructor.
        /// </summary>
        virtual ~StreamState() {}

        /// <summary>
        /// Perform the Play action based on the stream state.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="speed">The playback speed.</param>
        virtual bool Play(Controller& controller, int speed);

        /// <summary>
        /// Perform the Pause action based on the stream state.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        virtual void Pause(Controller& controller);

        /// <summary>
        /// Perform the Stop action based on the stream state.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        virtual void Stop(Controller& controller);

        /// <summary>
        /// Perform the GoToLive action based on the stream state.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        virtual bool GoToLive(Controller& controller);

        /// <summary>
        /// Perform the Seek action based on the stream state.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="unixTime">The start time for playback.</param>
        /// <param name="speed">The playback speed.</param>
        virtual bool Seek(Controller& controller, unsigned int unixTime, int speed);

    protected:
        /// <summary>
        /// Constructor.
        /// </summary>
        StreamState();

        /// <summary>
        /// Set the current state of the stream.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="state">The state to set the stream to.</param>
        void SetState(Controller& controller, StreamState* state);
    };

    /// <summary>
    /// Represents a stream that is currently in the playing state.
    /// </summary>
    class PlayingState : public StreamState {
    public:

        /// <summary>
        /// Virtual destructor.
        /// </summary>
        virtual ~PlayingState() {}
        virtual bool Play(Controller& controller, int speed) override;
        virtual void Pause(Controller& controller) override;
        virtual void Stop(Controller& controller) override;
        virtual bool GoToLive(Controller& controller) override;
        virtual bool Seek(Controller& controller, unsigned int unixTime, int speed) override;
    };

    /// <summary>
    /// Represents a stream that is currently in the paused state.
    /// </summary>
    class PausedState : public StreamState {
    public:

        /// <summary>
        /// Virtual destructor.
        /// </summary>
        virtual ~PausedState() {}
        virtual bool Play(Controller& controller, int speed) override;
        virtual void Stop(Controller& controller) override;
        virtual bool GoToLive(Controller& controller) override;
        virtual bool Seek(Controller& controller, unsigned int unixTime, int speed) override;
    };

    /// <summary>
    /// Represents a stream that is currently in the stopped state.
    /// </summary>
    class StoppedState : public StreamState {
    public:

        /// <summary>
        /// Virtual destructor.
        /// </summary>
        virtual ~StoppedState() {}
        virtual bool Play(Controller& controller, int speed) override;
        virtual bool GoToLive(Controller& controller) override;
        virtual bool Seek(Controller& controller, unsigned int unixTime, int speed) override;
    };
}
#endif // StreamState_h__
namespace TronderLeikan.Application.Common.Interfaces;

// Marker for kommando med returverdi — gir type-inferens i ISender.Send<TResult>()
public interface ICommand<TResult> { }

// Marker for kommando uten returverdi — gir type-inferens i ISender.Send()
public interface ICommand { }

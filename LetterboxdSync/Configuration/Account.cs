using System;

namespace LetterboxdSync.Configuration;

public class Account
{
    public string? UserJellyfin { get; set; }

    public string? UserLetterboxd { get; set; }

    public string? PasswordLetterboxd { get; set; }

    public bool Enable { get; set; }

    public bool SendFavorite { get; set; }

    public bool EnableDateFilter { get; set; }

    public int DateFilterDays { get; set; } = 7;
}

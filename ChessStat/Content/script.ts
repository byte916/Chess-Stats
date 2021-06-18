$(document).ready(() => {
    $('.ui.accordion').accordion();

    // Включаем анимацию для цифр общей статистики
    $('.count').each(function () {
        $(this).prop('Counter', 0).animate({
            Counter: $(this).text()
        },
            {
                duration: 1000,
                easing: 'swing',
                step: function (now) {
                    this.innerHTML = Math.ceil(now).toString().replace(/(\d)(?=(\d{3})+(\D|$))/g, '$1&thinsp;');
                }
            });
    });

    // Клик на кнопке Поиск в главном блоке
    $(".main-seach-button").on("click", () => {
        $(".dimmer").dimmer({
            closable: false
        }).dimmer("show");

        var id = $(".main-chess-id").val();
        loadResults(id,
            // Успешная загрузка
            () => {
                setTimeout(() => {
                    $(".contacts").hide();
                    $(".dimmer").dimmer("hide");
                    $("#main-search").css("display", "none");
                    $("#content").fadeIn(100);
                }, 300);
                $(".head-chess-id").val(id);
            },
            // Не успешная загрузка
            () => {
                setTimeout(() => {
                    $(".dimmer").dimmer("hide");
                }, 300);
            })
    })

    $(".head-seach-button").on("click", (item) => {
        $(".dimmer").dimmer({
            closable: false
        }).dimmer("show");
        loadResults($(item.target).parent().children(".head-chess-id").val(),
            // Успешная загрузка
            () => {
                setTimeout(() => {
                    $(".dimmer").dimmer("hide");
                }, 300);
            },
            // Не успешная загрузка
            () => {
                setTimeout(() => {
                    $(".dimmer").dimmer("hide");
                }, 300);
            })
    })

    $("#timeControls").on("click", (item) => {
        if (item.target.classList.contains("active")) return;
        let timeControl = item.target.innerText == 'Все' ? null : item.target.innerText;
        $(".dimmer").dimmer({
            closable: false
        }).dimmer("show");
        loadResults($(".head-chess-id").val(),
            // Успешная загрузка
            () => {
                setTimeout(() => {
                    $(".dimmer").dimmer("hide");
                }, 300);
                $("#timeControls .active").removeClass("active green");
                item.target.classList.add("active");
                item.target.classList.add("green");
            },
            // Не успешная загрузка
            () => {
                setTimeout(() => {
                    $(".dimmer").dimmer("hide");
                }, 300);
            }, timeControl)
    })
})

function loadResults(chessId, successCallBack, errorCallBack, timeControl?) {
    let tcParam = '';
    if (timeControl != null) {
        tcParam = "&timeControl=" + timeControl;
    }
    $.ajax({
        url: 'stat',
        data: "chessId=" + chessId + tcParam,
        success: function (result) {
            if (result == null || result == 'null') {
                $('body').toast({
                        displayTime: 10000,
                        class: 'error',
                        message: `Произошла ошибка. Попробуйте позже или напишите мне <a href='mailto:byte916@yandex.ru'>по почте</a> или <a href='https://vk.com/id2962981' target='_blank'>во вконтакте</a>`
                    });
                return errorCallBack();
            }
            successCallBack();
            showResult(result);
            if (timeControl == null) fillTimeControls(result.timeControls);
        },
        error: function () {
            errorCallBack();
            $('body').toast({
                    displayTime: 10000,
                    class: 'error',
                    message: `Произошла ошибка. Попробуйте позже или напишите мне <a href='mailto:byte916@yandex.ru'>по почте</a> или <a href='https://vk.com/id2962981' target='_blank'>во вконтакте</a>`
                });
        }
    });
}

function showResult(result) {
    fillOpponents(result.rivals);
    fillCommonStats(result.info);
    fillColorStrength(result.gameStrengths);
    fillHardestGames(result.hardestRivals);
    fillTournamentsStats(result.tournamentStats);
    fillInconenientOpponents(result.inconvenientOpponent);
    fillCurrentTournament(result.currentTournament)
}

function fillCommonStats(commonStats) {
    $("#name").html(commonStats.name);
    $("#maxRate").html('<h3 style="margin-bottom:0;">Максимальный рейтинг: ' + commonStats.maxRate + '</h3><small>(' + commonStats.maxDate + ')</small>');
    $("#games").text(commonStats.games);
    $("#wins").text(commonStats.wins);
    $("#draws").text(commonStats.draws);
    $("#loses").text(commonStats.loses);
}

function fillOpponents(rivals) {
    // Заполняем топ-частых соперников
    var opponentsTable = $("#opponents");
    opponentsTable.html("");
    var index = 1;
    rivals.forEach(rival => {
        // Если процент набранных очков 50% или больше - подсвечиваем зеленым
        var isGoodResult = rival.wins + rival.draws / 2 >= rival.games / 2
        var row = $("<tr/>");
        if (isGoodResult) row.addClass("left marked green");
        else row.addClass("left marked red");
        row.append(addCell('#' + index++));

        row.append(addCell('<a href="' + getRivalHref(rival.id) + '" target="_blank">' + rival.name + '</a>'));
        row.append(addCell(rival.games));
        row.append(addCell(rival.wins == 0 ? '' : rival.wins, rival.wins > rival.draws && rival.wins > rival.loses, "green"));
        row.append(addCell(rival.draws == 0 ? '' : rival.draws, rival.draws > rival.wins && rival.draws > rival.loses, "orange"));
        row.append(addCell(rival.loses == 0 ? '' : rival.loses, rival.loses > rival.draws && rival.loses > rival.wins, "red"));
        opponentsTable.append(row);
    });
}

function fillTimeControls(timeControls: string[]) {
    const block = $("#timeControls");
    block.html('');
    if (timeControls.length == 1) return;
    block.append("<button class='ui active green button'>Все</button>")
    timeControls.forEach(t => {
        
        block.append("<button class='ui button'>" + t +"</button>")
    })
}

function fillColorStrength(gameStrengths) {
    var gameStrengthsTable = $("#gameStrengths");
    gameStrengthsTable.html("");

    var totalWinsWhite = 0;
    var totalWinsBlack = 0;
    var totalDrawsWhite = 0;
    var totalDrawsBlack = 0;
    var totalLosesWhite = 0;
    var totalLosesBlack = 0;

    gameStrengths.forEach(rivals => {
        var row = $("<tr/>");
        switch (rivals.strength) {
            case 0:
                row.append("<td style='text-align: left;'><strong>Слабые</strong></td>");
                break;
            case 1:
                row.append("<td style='text-align: left;'><strong>Равные</strong></td>");
                break;
            case 2:
                row.append("<td style='text-align: left;'><strong>Сильные</strong></td>");
                break;
        }
        var whitePointPercent = Math.round(rivals.white.points * 100 / rivals.white.games);
        var blackPointPercent = Math.round(rivals.black.points * 100 / rivals.black.games);
        totalWinsWhite += rivals.white.wins;
        totalWinsBlack += rivals.black.wins;
        totalDrawsWhite += rivals.white.draws;
        totalDrawsBlack += rivals.black.draws;
        totalLosesWhite += rivals.white.loses;
        totalLosesBlack += rivals.black.loses;
        var totalWhite = rivals.white.wins + rivals.white.draws + rivals.white.loses;
        var totalBlack = rivals.black.wins + rivals.black.draws + rivals.black.loses;

        row.append(addCell(totalWhite));
        row.append(addCell(totalBlack, null, null, 'lightGray'));

        var isWhiteWinsBest = isBestValue(rivals.white.wins, rivals.black.wins, totalWhite, totalBlack);
        var isWhiteDrawBest = isBestValue(rivals.white.draws, rivals.black.draws, totalWhite, totalBlack);
        var isWhiteLoseBest = !isBestValue(rivals.white.loses, rivals.black.loses, totalWhite, totalBlack);
        var isWhitePercentBest = whitePointPercent > blackPointPercent;

        row.append(addCell(rivals.white.wins, isWhiteWinsBest, isWhiteWinsBest ? 'green' : ''));
        row.append(addCell(rivals.black.wins, !isWhiteWinsBest, !isWhiteWinsBest ? 'green' : '', 'lightGray'));
        row.append(addCell(rivals.white.draws, isWhiteDrawBest, isWhiteDrawBest ? 'orange' : ''));
        row.append(addCell(rivals.black.draws, !isWhiteDrawBest, !isWhiteDrawBest ? 'orange' : '', 'lightGray'));
        row.append(addCell(rivals.white.loses, isWhiteLoseBest, isWhiteLoseBest ? 'red' : ''));
        row.append(addCell(rivals.black.loses, !isWhiteLoseBest, !isWhiteLoseBest ? 'red' : '', 'lightGray'));
        row.append(addCell(whitePointPercent + "%", isWhitePercentBest, isWhitePercentBest ? 'green' : ''));
        row.append(addCell(blackPointPercent + "%", !isWhitePercentBest, !isWhitePercentBest ? 'green' : '', 'lightGray'));
        gameStrengthsTable.append(row);
    })

    var totalWhiteGames = totalWinsWhite + totalDrawsWhite + totalLosesWhite;
    var totalBlackGames = totalWinsBlack + totalDrawsBlack + totalLosesBlack;
    var whitePointPercent = Math.ceil((totalWinsWhite + totalDrawsWhite / 2) * 100 / (totalWinsWhite + totalDrawsWhite + totalLosesWhite));
    var blackPointPercent = Math.ceil((totalWinsBlack + totalDrawsBlack / 2) * 100 / (totalWinsBlack + totalDrawsBlack + totalLosesBlack));

    var isWhiteWinsBest = isBestValue(totalWinsWhite, totalWinsBlack, totalWhiteGames, totalBlackGames);
    var isWhiteDrawBest = isBestValue(totalDrawsWhite, totalDrawsBlack, totalWhiteGames, totalBlackGames);
    var isWhiteLoseBest = !isBestValue(totalLosesWhite, totalLosesBlack, totalWhiteGames, totalBlackGames);
    var isWhitePercentBest = whitePointPercent > blackPointPercent;

    var totalRow = $("<tr/>");
    totalRow.append("<td style='text-align: left;'><strong>Итого</strong></td>");
    totalRow.append(addCell(totalWhiteGames));
    totalRow.append(addCell(totalBlackGames, null, null, 'lightGray'));
    totalRow.append(addCell(totalWinsWhite, totalWinsWhite > totalWinsBlack, totalWinsWhite > totalWinsBlack ? 'green' : ''));
    totalRow.append(addCell(totalWinsBlack, totalWinsWhite < totalWinsBlack, totalWinsWhite < totalWinsBlack ? 'green' : '', 'lightGray'));
    totalRow.append(addCell(totalDrawsWhite, totalDrawsWhite > totalDrawsBlack, totalDrawsWhite > totalDrawsBlack ? 'orange' : ''));
    totalRow.append(addCell(totalDrawsBlack, totalDrawsWhite < totalDrawsBlack, totalDrawsWhite < totalDrawsBlack ? 'orange' : '', 'lightGray'));
    totalRow.append(addCell(totalLosesWhite, totalLosesWhite < totalLosesBlack, totalLosesWhite < totalLosesBlack ? 'red' : ''));
    totalRow.append(addCell(totalLosesBlack, totalLosesWhite > totalLosesBlack, totalLosesWhite > totalLosesBlack ? 'red' : '', 'lightGray'));
    totalRow.append(addCell(whitePointPercent + "%", isWhitePercentBest, isWhitePercentBest ? 'green' : ''));
    totalRow.append(addCell(blackPointPercent + "%", !isWhitePercentBest, !isWhitePercentBest ? 'green' : '', 'lightGray'));
    gameStrengthsTable.append(totalRow);
}
// Проверяет, является ли текущий показатель лучше другого, учитывая соотношения общего количества игр.
function isBestValue(current, other, currentTotal, otherTotal) {
    if (currentTotal == 0 && otherTotal == 0) return null;
    if (currentTotal == 0) currentTotal = 1;
    if (otherTotal == 0) otherTotal = 1;
    return current / currentTotal >= other / otherTotal;
}

function getRivalHref(id) {
    if (id == null) return id;
    if (id.indexOf("ratings.fide") == -1) return 'https://ratings.ruchess.ru/people/' + id;
    return id;
}

function fillHardestGames(hardestRivals) {
    // Заполняем топ-сложных соперников
    var hardestGames = $("#hardestGames");
    hardestGames.html("");
    let index = 1;
    hardestRivals.forEach(rival => {
        var row = $("<tr/>");
        row.append(addCell('#' + index++));
        row.append(addCell('<a href="' + getRivalHref(rival.id) + '" target="_blank">' + rival.name + '</a>'));
        row.append(addCell(rival.playerElo));
        row.append(addCell(rival.elo));
        row.append(addCell(rival.date));
        row.append(addCell(rival.tournament));
        row.append(addCell(rival.color, null, null, rival.color == "Черные" ? "lightGray" : null));
        row.append(addCell(rival.result, null, null, rival.result == "1" ? "green" : "olive"))
        hardestGames.append(row);
    });
}
function fillTournamentsStats(tournamentStats) {
    // Сила игры по ходу турнира
    var toursStats = $("#toursStats");
    toursStats.html("");
    tournamentStats.forEach(tours => {
        toursStats.append("<strong>Турнир из " + tours.length + " туров</strong>")
        var table = $('<table class="ui unstackable selectable right aligned table">');
        var thead = $('<thead>');
        var tbody = $('<tbody>');
        var tourRow = $('<tr>');
        var eloRow = $('<tr>');
        var percentRow = $('<tr>');
        var winsRow = $('<tr>');
        var drawsRow = $('<tr>');
        var losesRow = $('<tr>');
        var gamesRow = $('<tr>');

        var index = 1;
        tourRow.append('<th></th>');
        eloRow.append('<td>Средний ЭЛО соперников</td>');
        percentRow.append('<td>% набранных очков</td>');
        winsRow.append('<td>Побед</td>');
        drawsRow.append('<td>Ничьих</td>');
        losesRow.append('<td>Поражений</td>');
        gamesRow.append('<td>Всего Игр</td>');

        tours.forEach(tour => {
            tourRow.append('<th>' + index++ + '</th>');
            eloRow.append('<td>' + tour.avgElo + '</td>');

            var percentColor = ''
            if (tour.pointPercent >= 75) percentColor = 'green';
            else if (tour.pointPercent >= 50) percentColor = 'olive';
            else if (tour.pointPercent >= 25) percentColor = 'yellow';
            else percentColor = 'orange';
            percentRow.append('<td class="' + percentColor + '">' + tour.pointPercent + '%</td>');
            winsRow.append('<td>' + tour.wins + '</td>');
            drawsRow.append('<td>' + tour.draws + '</td>');
            losesRow.append('<td>' + tour.loses + '</td>');
            gamesRow.append('<td>' + tour.games + '</td>');
        })
        thead.append(tourRow);
        tbody.append(eloRow);
        tbody.append(percentRow);
        tbody.append(winsRow);
        tbody.append(drawsRow);
        tbody.append(losesRow);
        tbody.append(gamesRow);

        table.append(thead);
        table.append(tbody);

        toursStats.append(table);
    })
}
function fillInconenientOpponents(inconvenientOpponent) {
    // Неудобные соперники
    var inconvenientOpponentTable = $("#inconvenientOpponent");
    inconvenientOpponentTable.html('');
    let index = 1;
    inconvenientOpponent.forEach(op => {
        var row = $("<tr>");
        row.append(addCell(index++));
        row.append(addCell('<a href="' + getRivalHref(op.id) + '" target="_blank">' + op.name + '</a>'));
        row.append(addCell(Math.ceil(100 * op.points / op.games)));
        row.append(addCell(op.wins == 0 ? '' : op.wins, null, "green"));
        row.append(addCell(op.draws == 0 ? '' : op.draws, null, "orange"));
        row.append(addCell(op.loses == 0 ? '' : op.loses, null, "red"));
        row.append(addCell(op.games));
        inconvenientOpponentTable.append(row);
    })
}
function addCell(data, isStrong?: boolean, colorText?: string, colorBack?: string, classes?: string) {
    var style = 'ui';
    if (colorText != null) style = style + ' text ' + colorText;

    var cell = $("<td/>");

    if (classes == null) classes = '';
    if (colorBack != null) cell.addClass(colorBack);
    cell.addClass(classes);
    if (isStrong) style = style + ' bold'
    data = "<span class='" + style + "'>" + data + "</strong>";
    cell.html(data);
    return cell;
}

function fillCurrentTournament(currentTournament) {
    var block = $("#lastTournament");
    var content = block.children("div");
    content.html('');
    content.prepend("<strong>" + currentTournament.name + "</strong> (" + currentTournament.date.trim() + ")");
    var tbody = block.find("table > tbody");
    tbody.html('');

    for (let i = 0; i < currentTournament.games.length-1; i++) {
        const game = currentTournament.games[i];
        
        var row = $("<tr>");
        row.append(addCell('<a href="' + getRivalHref(game.id) + '" target="_blank">' + game.name + '</a>'));
        var resultTextColor = '';
        var resultBackColor = '';
        if (game.color == "0") {
            resultTextColor = 'white';
            resultBackColor = 'black';
        } else {
            resultBackColor = 'white';
            resultTextColor = 'black';
        }

        row.append(addCell(game.rate, null, null, null, "right aligned"));

        var gameResult = game.result;
        if (game.result == 0.5) gameResult = '½';
        row.append(addCell(gameResult, null, resultTextColor, resultBackColor, "center aligned"));

        if (game.totalStat != null) {
            row.append(addCell(game.totalStat.wins == 0 ? '' : game.totalStat.wins, null, "green", null, "right aligned"));
            row.append(addCell(game.totalStat.draws == 0 ? '' : game.totalStat.draws, null, "orange", null, "right aligned"));
            row.append(addCell(game.totalStat.loses == 0 ? '' : game.totalStat.loses, null, "red", null, "right aligned"));
        }
        row.append(addCell(game.comment));
        tbody.append(row);
    }

    const totalRow = currentTournament.games[currentTournament.games.length - 1];
    var row = $("<tr>");
    row.append(addCell(totalRow.name, true));

    row.append(addCell(totalRow.rate, true, null, null, "right aligned"));

    var gameResult = totalRow.result;
    if (totalRow.result == 0.5) gameResult = '½';
    row.append(addCell(gameResult, true, null, null, "center aligned"));

    if (totalRow.totalStat != null) {
        row.append(addCell(totalRow.totalStat.wins == 0 ? '' : totalRow.totalStat.wins, true, "green", null, "right aligned"));
        row.append(addCell(totalRow.totalStat.draws == 0 ? '' : totalRow.totalStat.draws, true, "orange", null, "right aligned"));
        row.append(addCell(totalRow.totalStat.loses == 0 ? '' : totalRow.totalStat.loses, true, "red", null, "right aligned"));
    }

    row.append(addCell(''));
    tbody.append(row);
}
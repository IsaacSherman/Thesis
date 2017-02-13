function [ confusionMatrix, model, label ] = trainMulticlassNB( trX, teX,...
    trY, teY, predictorNames, numlearners,otherArgs )
%TRAINNB Summary of this function goes here
%   Detailed explanation goes here


rng(13, 'twister');
sprintf('numlearners = %d', numlearners)
sprintf('length of otherArgs = %d', length(otherArgs))
switch(length(otherArgs))
    case 2
        model = fitcnb(trX, trY, 'PredictorNames', predictorNames,...
            otherArgs{1}, otherArgs{2});
    case 4
        model = fitcnb(trX, trY, 'PredictorNames', predictorNames,...
            otherArgs{1}, otherArgs{2}, otherArgs{3}, otherArgs{4});
    case 6
        model = fitcnb(trX, trY, 'PredictorNames', predictorNames,...
            otherArgs{1}, otherArgs{2}, otherArgs{3},...
            otherArgs{4}, otherArgs{5}, otherArgs{6});
    case 8
        model = fitcnb(trX, trY, 'PredictorNames', predictorNames,...
            otherArgs{1}, otherArgs{2}, otherArgs{3},...
            otherArgs{4}, otherArgs{5}, otherArgs{6}, otherArgs{7}, otherArgs{8});
    case 10
        disp('Made it to case 10')
        model = fitcnb(trX, trY, 'PredictorNames', predictorNames, ...
            otherArgs{1}, otherArgs{2}, otherArgs{3},...
            otherArgs{4}, otherArgs{5}, otherArgs{6}, otherArgs{7}, otherArgs{8}, ...
            otherArgs{9}, otherArgs{10});
    otherwise
        model = fitcsvm(trX, trY, 'PredictorNames', predictorNames );
end

label = predict(model, teX);

confusionMatrix = confusionmat(teY, label);



end

